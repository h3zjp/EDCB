#pragma once

#include <windows.h>

class CPacketInit
{
public:
	CPacketInit(void);
	~CPacketInit(void);

	//入力バッファを188バイト単位のTSに変換し、188の倍数になるようにそろえる
	//戻り値：
	// TRUE（成功）、FALSE（失敗）
	//引数：
	// inData			[IN]入力TSデータ
	// inSize			[IN]inDataのサイズ（BYTE単位）
	// outData			[OUT]188バイトに整列したバッファ（次回呼び出しまで保持）
	// outSize			[OUT]outDataのサイズ（BYTE単位）
	BOOL GetTSData(
		BYTE* inData,
		DWORD inSize,
		BYTE** outData,
		DWORD* outSize
		);

	//内部バッファのクリア
	void ClearBuff();

protected:
	vector<BYTE> outBuff;
	BYTE nextStartBuff[256];
	DWORD nextStartSize;

	DWORD packetSize;
};
