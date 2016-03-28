#pragma once

#include "Util.h"
#include "StringUtil.h"

template <class K, class V>
class CParseText
{
public:
	CParseText() {}
	virtual ~CParseText() {}
	bool ParseText(LPCWSTR filePath = NULL);
	const map<K, V>& GetMap() const { return this->itemMap; }
	const wstring& GetFilePath() const { return this->filePath; }
	void SetFilePath(LPCWSTR filePath) { this->filePath = filePath; }
protected:
	bool SaveText() const;
	virtual bool ParseLine(const wstring& parseLine, pair<K, V>& item) = 0;
	virtual bool SaveLine(const pair<K, V>& item, wstring& saveLine) const { return false; }
	virtual bool SaveFooterLine(wstring& saveLine) const { return false; }
	virtual bool SelectIDToSave(vector<K>& sortList) const { return false; }
	map<K, V> itemMap;
	wstring filePath;
};

template <class K, class V>
bool CParseText<K, V>::ParseText(LPCWSTR filePath)
{
	this->itemMap.clear();
	if( filePath != NULL ){
		this->filePath = filePath;
	}
	if( this->filePath.empty() ){
		return false;
	}
	HANDLE hFile;
	for( int retry = 0;; ){
		hFile = CreateFile(this->filePath.c_str(), GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
		if( hFile != INVALID_HANDLE_VALUE ){
			break;
		}else if( GetLastError() == ERROR_FILE_NOT_FOUND ){
			return true;
		}else if( ++retry > 5 ){
			//6回トライしてそれでもダメなら失敗
			OutputDebugString(L"CParseText<>::ParseText(): Error: Cannot open file\r\n");
			return false;
		}
		Sleep(200 * retry);
	}

	vector<char> buf;
	vector<WCHAR> parseBuf;
	wstring parseLine;
	for(;;){
		//4KB単位で読み込む
		buf.resize(buf.size() + 4096);
		DWORD dwRead;
		if( ReadFile(hFile, &buf.front() + buf.size() - 4096, 4096, &dwRead, NULL) == FALSE || dwRead == 0 ){
			buf.resize(buf.size() - 4096);
			buf.push_back('\0');
		}else{
			buf.resize(buf.size() - 4096 + dwRead);
		}
		//完全に読み込まれた行をできるだけ解析
		size_t offset = 0;
		for( size_t i = 0; i < buf.size(); i++ ){
			bool eof = buf[i] == '\0';
			if( eof || buf[i] == '\r' && i + 1 < buf.size() && buf[i + 1] == '\n' ){
				buf[i] = '\0';
				size_t len = AtoW(&buf[offset], i - offset, parseBuf);
				parseLine.assign(parseBuf.begin(), parseBuf.begin() + len);
				pair<K, V> item;
				if( ParseLine(parseLine, item) ){
					this->itemMap.insert(item);
				}
				if( eof ){
					offset = i;
					break;
				}
				offset = (++i) + 1;
			}
		}
		buf.erase(buf.begin(), buf.begin() + offset);
		if( buf.empty() == false && buf[0] == '\0' ){
			break;
		}
	}
	CloseHandle(hFile);
	return true;
}

template <class K, class V>
bool CParseText<K, V>::SaveText() const
{
	if( this->filePath.empty() ){
		return false;
	}
	HANDLE hFile;
	for( int retry = 0;; ){
		hFile = _CreateDirectoryAndFile(this->filePath.c_str(), GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
		if( hFile != INVALID_HANDLE_VALUE ){
			break;
		}else if( ++retry > 5 ){
			OutputDebugString(L"CParseText<>::SaveText(): Error: Cannot open file\r\n");
			return false;
		}
		Sleep(200 * retry);
	}

	wstring saveLine;
	vector<char> saveBuf;
	vector<K> idList;
	if( SelectIDToSave(idList) ){
		for( size_t i = 0; i < idList.size(); i++ ){
			map<K, V>::const_iterator itr = this->itemMap.find(idList[i]);
			saveLine.clear();
			if( itr != this->itemMap.end() && SaveLine(*itr, saveLine) ){
				saveLine += L"\r\n";
				size_t len = WtoA(saveLine.c_str(), saveLine.size(), saveBuf);
				DWORD dwWrite;
				WriteFile(hFile, &saveBuf.front(), (DWORD)len, &dwWrite, NULL);
			}
		}
	}else{
		for( map<K, V>::const_iterator itr = this->itemMap.begin(); itr != this->itemMap.end(); itr++ ){
			saveLine.clear();
			if( SaveLine(*itr, saveLine) ){
				saveLine += L"\r\n";
				size_t len = WtoA(saveLine.c_str(), saveLine.size(), saveBuf);
				DWORD dwWrite;
				WriteFile(hFile, &saveBuf.front(), (DWORD)len, &dwWrite, NULL);
			}
		}
	}
	saveLine.clear();
	if( SaveFooterLine(saveLine) ){
		saveLine += L"\r\n";
		size_t len = WtoA(saveLine.c_str(), saveLine.size(), saveBuf);
		DWORD dwWrite;
		WriteFile(hFile, &saveBuf.front(), (DWORD)len, &dwWrite, NULL);
	}
	CloseHandle(hFile);
	return true;
}
