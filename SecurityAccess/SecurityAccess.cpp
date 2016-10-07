// SecurityAccess.cpp : 定义 DLL 应用程序的导出函数。
//

#include "stdafx.h"
#include "SecurityAccess.h"


#include <stdio.h>
#include <stdlib.h>
#include <ctype.h>
#include <iostream>

UINT32 SecurityAccess_S300(UINT32 Seed, UINT8 access)
{
	UINT32 wSubSeed;
	UINT32 wMiddle;
	UINT32 wLastBit;
	UINT32 wLeft31Bits;
	UINT32 EncryptConstant = 0x5f9ea12a;
	UINT32 Key;
	UINT8  counter, i, DB1, DB2, DB3;
	UINT16 middle;

	wSubSeed = Seed;

	if (access == 0x01)
	{
		EncryptConstant = 0x5f9ea12a;
	}
	else if (access == 0x03)
	{
		EncryptConstant = 0xdcdfe990;
	}
	else if (access == 0x05)
	{
		EncryptConstant = 0x5d9a91ca;
	}
	else if (access == 0x07)
	{
		EncryptConstant = 0xdada7a30;
	}

	middle = (UINT16)((EncryptConstant & 0x00001000) >> 11) | ((EncryptConstant & 0x00400000) >> 22);
	switch (middle)
	{
	case 0:
		wMiddle = (UINT8)(Seed & 0x000000ff);
		break;
	case 1:
		wMiddle = (UINT8)((Seed & 0x0000ff00) >> 8);
		break;
	case 2:
		wMiddle = (UINT8)((Seed & 0x00ff0000) >> 16);
		break;
	case 3:
		wMiddle = (UINT8)((Seed & 0xff000000) >> 24);
		break;
	}

	DB1 = (UINT8)((EncryptConstant & 0x000007F8) >> 3);
	DB2 = (UINT8)(((EncryptConstant & 0x7F800000) >> 23) ^ 0xA5);
	DB3 = (UINT8)(((EncryptConstant & 0x003FC000) >> 14) ^ 0x5A);

	counter = (UINT32)(((wMiddle ^ DB1) & DB2) + DB3);

	for (i = 0; i < counter; i++)
	{
		wMiddle = ((wSubSeed & 0x20000000) / 0x20000000) ^ ((wSubSeed & 0x01000000) / 0x01000000)
			^ ((wSubSeed & 0x2000) / 0x2000) ^ ((wSubSeed & 0x08) / 0x08);

		wLastBit = (wMiddle & 0x00000001);

		wSubSeed = (UINT32)(wSubSeed << 1);
		wLeft31Bits = (UINT32)(wSubSeed & 0xFFFFFFFE);
		wSubSeed = (UINT32)(wLeft31Bits | wLastBit);
	}

	if (EncryptConstant & 0x00000002)
	{
		wLeft31Bits = ((wSubSeed & 0x00FF0000) >> 8) |
			((wSubSeed & 0xFF000000) >> 24) |
			((wSubSeed & 0x000000FF) << 16) | ((wSubSeed & 0x0000FF00) << 16);
	}
	else
		wLeft31Bits = wSubSeed;

	Key = wLeft31Bits ^ EncryptConstant;

	return(Key);

}

UINT16 SecurityAccess_M16(UINT16 seed, UINT8 mode)
{
#define TOPBIT              0x8000
#define POLYNOM_1           0x1021      /* CRC-CCITT  (Atech ADJ mode) */
#define POLYNOM_1B          0x8408      /* CRC-XMODEM (Chery EOL mode) */
#define POLYNOM_2           0x8025      /* non-standard */
#define BITMASK             0x0080      /* non-standard */
#define INITIAL_REMINDER    0xFFFE      /* non-standard */
#define MSG_LEN             2           /* seed length in UINT8s */

#define ADJ                 5

	UINT8 bSeed[2];
	UINT16 remainder;
	UINT16 polynom_1;
	UINT8 n;
	UINT8 i;

	bSeed[0] = (UINT8)(seed >> 8); /* MSB */
	bSeed[1] = (UINT8)seed; /* LSB */
	remainder = INITIAL_REMINDER;

	if (mode == ADJ)
	{
		polynom_1 = POLYNOM_1;
	}
	else
	{
		polynom_1 = POLYNOM_1B;
	}

	for (n = 0; n < MSG_LEN; n++)
	{
		/* Bring the next UINT8 into the remainder. */
		remainder ^= ((bSeed[n]) << 8);

		/* Perform modulo-2 division, a bit at a time. */
		for (i = 0; i < 8; i++)
		{
			/* Try to divide the current data bit. */
			if (remainder & TOPBIT)
			{
				if (remainder & BITMASK)
				{
					remainder = (remainder << 1) ^ polynom_1;
				}
				else
				{
					remainder = (remainder << 1) ^ POLYNOM_2;
				}
			}
			else
			{
				remainder = (remainder << 1);
			}
		}
	}
	/* The final remainder is the key */
	return remainder;
}

SECURITYACCESS_API
UINT32 __cdecl SecurityAccess(UINT32 project, UINT32 seed, UINT32 level)
{
	switch (project)
	{
	case 0:
		return SecurityAccess_S300(seed, level);
		break;
	case 1:return SecurityAccess_M16(seed, level);
		return 0;
	default:
		break;
	}
	return 0;
}
