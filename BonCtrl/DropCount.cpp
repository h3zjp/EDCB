#include "stdafx.h"
#include "DropCount.h"
#include "../Common/StringUtil.h"
#include "../Common/TimeUtil.h"
#include <stdio.h>


CDropCount::CDropCount(void)
{
	this->drop = 0;
	this->scramble = 0;
	this->lastLogTime = 0;

	this->lastLogDrop = 0;
	this->lastLogScramble = 0;

	this->signalLv = 0;
	this->bonFile = L"";
}

void CDropCount::AddData(const BYTE* data, DWORD size)
{
	if( data == NULL || size == 0 ){
		return ;
	}
	DROP_INFO item = {};
	for( DWORD i=0; i<size; i+=188 ){
		BYTE sync_byte = data[i];
		BYTE transport_error_indicator = data[i + 1] & 0x80;
		if( sync_byte == 0x47 && transport_error_indicator == 0 ){
			item.PID = (data[i + 1] << 8 | data[i + 2]) & 0x1FFF;
			vector<DROP_INFO>::iterator itr;
			itr = std::lower_bound(this->infoList.begin(), this->infoList.end(), item,
			                       [](const DROP_INFO& a, const DROP_INFO& b) { return a.PID < b.PID; });
			if( itr == this->infoList.end() || itr->PID != item.PID ){
				BYTE continuity_counter = data[i + 3] & 0x0F;
				item.lastCounter = (continuity_counter + 15) & 0x0F;
				itr = this->infoList.insert(itr, item);
			}
			itr->total++;
			if( itr->PID != 0x1FFF ){
				CheckCounter(data + i, &(*itr));
			}
		}
	}
	DWORD tick = GetTickCount();
	if( tick - this->lastLogTime > 5000 ){
		if( this->lastLogDrop < this->drop ||
		    this->lastLogScramble < this->scramble ){
			string logline;
			SYSTEMTIME now;
			ConvertSystemTime(GetNowI64Time(), &now);
			Format(logline, "%04d/%02d/%02d %02d:%02d:%02d Drop:%I64d Scramble:%I64d Signal: %.02f\r\n",
				now.wYear,
				now.wMonth,
				now.wDay,
				now.wHour,
				now.wMinute,
				now.wSecond,
				this->drop,
				this->scramble,
				this->signalLv
				);
			this->log += logline;
			this->lastLogDrop = max(this->drop, this->lastLogDrop);
			this->lastLogScramble = max(this->scramble, this->lastLogScramble);
		}
		this->lastLogTime = tick;
	}
}

void CDropCount::Clear()
{
	this->infoList.clear();
	this->drop = 0;
	this->scramble = 0;
	this->log.clear();
	this->lastLogTime = 0;

	if( this->lastLogDrop != ULLONG_MAX ){
		this->lastLogDrop = 0;
	}
	if( this->lastLogScramble != ULLONG_MAX ){
		this->lastLogScramble = 0;
	}
	this->signalLv = 0;
}

void CDropCount::SetSignal(float level)
{
	this->signalLv = level;
}

void CDropCount::SetBonDriver(const wstring& bonDriver)
{
	this->bonFile = bonDriver;
}

void CDropCount::SetNoLog(BOOL noLogDrop, BOOL noLogScramble)
{
	this->lastLogDrop = noLogDrop ? ULLONG_MAX : this->lastLogDrop == ULLONG_MAX ? 0 : this->lastLogDrop;
	this->lastLogScramble = noLogScramble ? ULLONG_MAX : this->lastLogScramble == ULLONG_MAX ? 0 : this->lastLogScramble;
}

void CDropCount::GetCount(ULONGLONG* drop_, ULONGLONG* scramble_)
{
	if( drop_ != NULL ){
		*drop_ = this->drop;
	}
	if( scramble_ != NULL ){
		*scramble_ = this->scramble;
	}
}

ULONGLONG CDropCount::GetDropCount()
{
	return this->drop;
}

ULONGLONG CDropCount::GetScrambleCount()
{
	return this->scramble;
}

void CDropCount::CheckCounter(const BYTE* packet, DROP_INFO* info)
{
	BYTE transport_scrambling_control = packet[3] >> 6;
	BYTE adaptation_field_control = (packet[3] >> 4) & 0x03;
	BYTE continuity_counter = packet[3] & 0x0F;

	if( transport_scrambling_control != 0 ){
		info->scramble++;
		this->scramble++;
	}
	
	if( adaptation_field_control == 0x00 || adaptation_field_control == 0x02 ){
		//ペイロードが存在しない場合は意味なし
		info->duplicateFlag = FALSE;
	}else{
		BYTE adaptation_field_length = packet[4];
		BYTE discontinuity_indicator = packet[5] & 0x80;
		if( info->lastCounter == continuity_counter ){
			if( adaptation_field_control == 0x01 || adaptation_field_length == 0 || discontinuity_indicator == 0 ){
				//※厳密には重送判定は前パケットとの完全比較もすべき
				if( info->duplicateFlag == FALSE ){
					//重送？一応連続と判定
					info->duplicateFlag = TRUE;
				}else{
					//前回重送と判断してるので不連続
					info->drop++;
					this->drop++;
				}
			}else{
				//不連続の判定だが正常
				info->duplicateFlag = FALSE;
			}
		}else{
			//※原作はたぶんlastCounter==15またはcontinuity_counter==0のときの連続判定がバグっていた
			if( ((info->lastCounter + 1) & 0x0F) != continuity_counter ){
				if( adaptation_field_control == 0x01 || adaptation_field_length == 0 || discontinuity_indicator == 0 ){
					//カウンターが飛んだので不連続
					//※原作はここで差分を加算する
					info->drop++;
					this->drop++;
				}
			}
			info->duplicateFlag = FALSE;
		}
	}

	info->lastCounter = continuity_counter;
}

void CDropCount::SaveLog(const wstring& filePath)
{
	//※原作と異なりディレクトリの自動生成はしない
	FILE* fp_;
	if( _wfopen_s(&fp_, filePath.c_str(), L"wbN") == 0 ){
		std::unique_ptr<FILE, decltype(&fclose)> fp(fp_, fclose);
		fprintf_s(fp.get(), "%s\r\n", this->log.c_str());

		for( vector<DROP_INFO>::const_iterator itr = this->infoList.begin(); itr != this->infoList.end(); itr++ ){
			LPCSTR desc = "";
			vector<pair<WORD, string>>::const_iterator itrPID;
			switch( itr->PID ){
			case 0x0000:
				desc = "PAT";
				break;
			case 0x0001:
				desc = "CAT";
				break;
			case 0x0010:
				desc = "NIT";
				break;
			case 0x0011:
				desc = "SDT/BAT";
				break;
			case 0x0012:
			case 0x0026:
			case 0x0027:
				desc = "EIT";
				break;
			case 0x0013:
				desc = "RST";
				break;
			case 0x0014:
				desc = "TDT/TOT";
				break;
			case 0x0017:
				desc = "DCT";
				break;
			case 0x001E:
				desc = "DIT";
				break;
			case 0x001F:
				desc = "SIT";
				break;
			case 0x0020:
				desc = "LIT";
				break;
			case 0x0021:
				desc = "ERT";
				break;
			case 0x0022:
				desc = "PCAT";
				break;
			case 0x0023:
			case 0x0028:
				desc = "SDTT";
				break;
			case 0x0024:
				desc = "BIT";
				break;
			case 0x0025:
				desc = "NBIT/LDT";
				break;
			case 0x0029:
				desc = "CDT";
				break;
			case 0x1FFF:
				desc = "NULL";
				break;
			default:
				itrPID = std::lower_bound(this->pidName.begin(), this->pidName.end(), std::make_pair(itr->PID, string()));
				if( itrPID != this->pidName.end() && itrPID->first == itr->PID ){
					desc = itrPID->second.c_str();
				}
				break;
			}
			fprintf_s(fp.get(), "PID: 0x%04X  Total:%9I64d  Drop:%9I64d  Scramble: %9I64d  %s\r\n",
			          itr->PID, itr->total, itr->drop, itr->scramble, desc);
		}

		string strA;
		WtoA(L"使用BonDriver : " + bonFile, strA);
		fprintf_s(fp.get(), "\r\n%s\r\n", strA.c_str());
	}
}

void CDropCount::SetPIDName(WORD pid, LPCSTR name)
{
	vector<pair<WORD, string>>::iterator itr;
	itr = std::lower_bound(this->pidName.begin(), this->pidName.end(), std::make_pair(pid, string()));
	if( itr == this->pidName.end() || itr->first != pid ){
		itr = this->pidName.insert(itr, std::make_pair(pid, string()));
	}
	itr->second = name;
}
