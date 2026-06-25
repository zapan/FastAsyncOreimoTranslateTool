#include <fstream>
#include <string>
#include <iostream>
#include <iomanip>
#include <cstdint>

using namespace std;

void DumpRemain(char*, ofstream&, int);
void WriteBGName(char, ofstream&);
void WriteCharacterName(char, ofstream&);
void WriteCG(char, ofstream&);
void WriteORE(char, ofstream&);
void WriteScript(short, ofstream&);
void WriteBGM(char, ofstream&);
void WriteCutin(char, ofstream&);
void WriteSE(char, ofstream&);
void WriteTsukkomi(char, ofstream&);

int main(int argc, const char* argv[])
{
	if (argc != 2)
	{
		cout << "usage: parseOBJ targetfile\n";
		return -1;
	}

	string LogName = argv[1];
	LogName += ".log";

	ifstream InFile(argv[1], ios::in | ios::binary );
	ofstream OutFile(LogName.c_str(), ios::out | ios::trunc);

	if (!InFile)
	{
		cout << "Unable to open input file " << argv[1] << endl;
		return -1;
	}

	if (!OutFile)
	{
		cout << "Unable to write log file " << LogName << endl;
		return -1;
	}
	
	//Skip header
	char Header[0x30];
	InFile.read(Header, 0x30);

	int TotalLines = 0;

	
	while(InFile)
	{
		uint32_t Size = 0;
				
		InFile.read((char*)&Size, 4);
		//Cheating end condition here
		if (Size == 0)
			break;

		TotalLines++;

		int Lines = Size / 0xF;

		OutFile << "Entry 0x" << hex << TotalLines << " 0x" << Size << " bytes\t";

		char *Buffer = new char[Size];

		InFile.seekg(-4,ios_base::cur);
		InFile.read(Buffer, Size);

		//Let's figure out what this line is
		unsigned char Type = Buffer[4];

		switch(Type)
		{
			case 0x00:
			{
				OutFile << "00 - set\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0x01:
			{
				OutFile << "01 - if\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0x08:
			{
				OutFile << "08\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0x14:
			{
				OutFile << "14 - ORE get\t";
				// I've seen 10 and 20 of these lines
				if (Size == 0x10)
				{
					WriteORE(Buffer[0xA], OutFile);
					OutFile << '\t';
				}
				else
				{
					OutFile << "20 size\t";
					DumpRemain(Buffer, OutFile, 27);
				}
				break;
			}

			case 0x15:
			{
				OutFile << "15 - ORE prompt\t";
				// I've seen 20 and 30 of these lines
				WriteORE(Buffer[0xA], OutFile);
				OutFile << '\t';

				char TempEntry[4] = {0x00, 0x00};
				for(int x=0; x<4; x++)
					TempEntry[x] = Buffer[0xE + x];

				uint32_t* Entry = reinterpret_cast<uint32_t*>(TempEntry);

				OutFile << "Branch offset 0x" << hex << *Entry << '\t';

				for(int x=0; x<4; x++)
					TempEntry[x] = Buffer[0x12 + x];

				Entry = reinterpret_cast<uint32_t*>(TempEntry);

				OutFile << "Dialogue 0x" << hex << *Entry << '\t';
				if (Size == 0x30)
				{
					WriteORE(Buffer[0x16], OutFile);
					OutFile << '\t';

					for(int x=0; x<4; x++)
						TempEntry[x] = Buffer[0x1A + x];

					uint32_t* Entry = reinterpret_cast<uint32_t*>(TempEntry);

					OutFile << "Branch offset 0x" << hex << *Entry << '\t';

					for(int x=0; x<4; x++)
						TempEntry[x] = Buffer[0x1E + x];

					Entry = reinterpret_cast<uint32_t*>(TempEntry);

					OutFile << "Dialogue 0x" << hex << *Entry << '\t';
				}
				break;
			}

			case 0x2C:
			{
				OutFile << "2C - show textbox\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0x2D:
			{
				OutFile << "2D - hide textbox\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}
		
			case 0x58:
			{
				OutFile << "58 - start BGM\t";
				WriteBGM(Buffer[6], OutFile);
				OutFile << '\t';
				break;
			}
		
			case 0x59:
			{
				OutFile << "59 - stop BGM\t";
				break;
			}

			case 0x5C:
			{
				OutFile << "5C - SE\t";
				WriteSE(Buffer[5], OutFile);
				OutFile << '\t';
				break;
			}

			case 0x64:
			{
				OutFile << "64 - dialogue\t";
				break;
			}

			case 0x68:
			{
				OutFile << "68 - DEBUG?\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0x6A:
			{
				OutFile << "6A\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0x78:
			{
				OutFile << "78 - tsukkomi\t";
				WriteTsukkomi(Buffer[6], OutFile);
				OutFile << "\tChose ";

				char TempEntry[2] = {0x00, 0x00};
				for(int x=0; x<2; x++)
					TempEntry[x] = Buffer[0xE + x];

				unsigned short* Entry = reinterpret_cast<unsigned short*>(TempEntry);

				OutFile << hex << *Entry << "\tNot ";

				for(int x=0; x<2; x++)
					TempEntry[x] = Buffer[0x12 + x];

				Entry = reinterpret_cast<unsigned short*>(TempEntry);
				OutFile << hex << *Entry << '\t';

				break;
			}

			case 0x90:
			{
				OutFile << "90 - shake screen\t";
				break;
			}

			case 0x91:
			{
				OutFile << "91 - cut fade out\t";
				break;
			}

			case 0x92:
			{
				OutFile << "92 - cut fade in\t";
				break;
			}

			case 0x97:
			{
				OutFile << "97\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xBC:
			{
				OutFile << "BC - Scene name\t";
				break;
			}

			case 0xBD:
			{
				OutFile << "BD\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xBE:
			{
				OutFile << "BE - branch internal\t";
				char TempEntry[4] = {0x00, 0x00};
				for(int x=0; x<4; x++)
					TempEntry[x] = Buffer[0x6+x];

				uint32_t* Entry = reinterpret_cast<uint32_t*>(TempEntry);

				OutFile << "0x" << hex << *Entry;
				break;
			}

			case 0xBF:
			{
				OutFile << "BF - branch file\t";
				char TempScript[2] = {0x00, 0x00};
				TempScript[0] = Buffer[6];
				TempScript[1] = Buffer[7];
				unsigned short* Script = reinterpret_cast<unsigned short*>(TempScript);
				WriteScript(*Script, OutFile);
				break;
			}

			case 0xC0:
			{
				OutFile << "C0\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xC2:
			{
				OutFile << "C2 - video\t";
				unsigned char Size = Buffer[6];
				OutFile << "namesize= " << (int)Size << '\t';
				char *FileName = new char[Size];
				for(int x=0; x<Size; x++)
					FileName[x] = Buffer[0xA+x*2];

				FileName[Size] = '\0';
				OutFile << FileName << '\t';
				break;
			}

			case 0xC8:
			{
				OutFile << "C8\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xC9:
			{
				OutFile << "C9\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xCA:
			{
				OutFile << "CA - fade in/out\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xCC:
			{
				OutFile << "CC - BG\t";
				WriteBGName(Buffer[6], OutFile);
				break;
			}

			case 0xCD:
			{
				OutFile << "CD - BG\t";
				WriteBGName(Buffer[6], OutFile);
				break;
			}

			case 0xD0:
			{
				OutFile << "D0 - BG\t";
				WriteBGName(Buffer[6], OutFile);
				break;
			}

			case 0xD1:
			{
				OutFile << "D1 - BG\t";
				WriteBGName(Buffer[6], OutFile);
				break;
			}

			case 0xD4:
			{
				OutFile << "D4 - CG\t";
				WriteCG(Buffer[6], OutFile);
				break;
			}

			case 0xD5:
			{
				OutFile << "D5 - CG\t";
				WriteCG(Buffer[6], OutFile);
				break;
			}

			case 0xD6:
			{
				OutFile << "D6\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xDC:
			{
				OutFile << "DC\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xE8:
			{
				OutFile << "E8 - image cutin\t";
				WriteCutin(Buffer[6], OutFile);
				OutFile << '\t';
				break;
			}

			
			case 0xEA:
			{
				OutFile << "EA\t";
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xEB:
			{
				OutFile << "EB\t";
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xF4:
			{
				OutFile << "F4 - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xF5:
			{
				OutFile << "F5 - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xF6:
			{
				OutFile << "F6 - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xF7:
			{
				OutFile << "F7 - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 27);
				break;
			}

			case 0xF8:
			{
				OutFile << "F8 - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xF9:
			{
				OutFile << "F9 - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xFA:
			{
				OutFile << "FA - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			case 0xFC:
			{
				OutFile << "FC - character\t";
				OutFile << hex << int(Buffer[6]) << '\t';
				WriteCharacterName(Buffer[6], OutFile);
				OutFile << '\t';
				DumpRemain(Buffer, OutFile, 11);
				break;
			}

			default:
			{
				OutFile << "unknown: " << hex << (int)Type << '\t';
				break;
			}

		}

		OutFile << endl << endl;

		delete [] Buffer;
	}

	OutFile << "Total Lines = " << dec << TotalLines << " 0x" << hex << TotalLines << endl;
	
	return 0;
}

void DumpRemain(char* Buffer, ofstream& OutFile, int Size)
{
	unsigned char Temp;
	for(int x=0; x<Size; x++)
	{
			Temp = Buffer[5+x];
			OutFile << hex << setw(2) <<  setfill('0') << (int)Temp;
	}
	OutFile << '\t';
}

void WriteBGName(char BG, ofstream& OutFile)
{
	switch (BG)
	{
	case 0x00:
		OutFile << "Kyousuke's room (day)";
		break;
	case 0x01:
		OutFile << "Kyousuke's room (sunset) - BG00B";
		break;
	case 0x02: 
		OutFile << "Kyousuke's room (blinds shut) - BG00C";
		break;
	case 0x03:
		OutFile << "Kyousuke's room (night) - BG00D";
		break;
	case 0x04:
		OutFile << "Kirino's room (day) - BG01A";
		break;
	case 0x05:
		OutFile << "Kirino's room (sunset) - BG01B";
		break;
	case 0x06:
		OutFile << "Kirino's room (blinds shut) - BG01C";
		break;
	case 0x07:
		OutFile << "Kirino's room (night) - BG01D";
		break;
	case 0x08:
		OutFile << "Living room (day) - BG02A";
		break;
	case 0x09:
		OutFile << "Living room (sunset) - BG02B";
		break;
	case 0x0A:
		OutFile << "Living room (blinds shut) - BG02C";
		break;
	case 0x0B:
		OutFile << "Living room (night) - BG02D";
		break;
	case 0x0C:
		OutFile << "Living room (blinds day) - BG02E";
		break;
	case 0x0D:
		OutFile << "Entrance - BG03A";
		break;
	case 0x0E:
		OutFile << "Entrance (night) - BG03B";
		break;
	case 0x0F:
		OutFile << "Entrance (?) - BG03C";
		break;
	case 0x10:
		OutFile << "Front gate (day) - BG04A";
		break;
	case 0x11:
		OutFile << "Front gate (sunset) - BG04B";
		break;
	case 0x12:
		OutFile << "Front gate (night) - BG04C";
		break;
	case 0x13:
		OutFile << "Bathrom - BG05C";
		break;
	case 0x14:
		OutFile << "Path with gate (day) - BG06A";
		break;
	case 0x15:
		OutFile << "Path with gate (sunset) - BG06B";
		break;
	case 0x16:
		OutFile << "Path with gate (night) - BG06C";
		break;
	case 0x17:
		OutFile << "Park (day) - BG07A";
		break;
	case 0x18:
		OutFile << "Park (sunset) - BG07B";
		break;
	case 0x19:
		OutFile << "Tamura front door (day) - BG08A";
		break;
	case 0x1A:
		OutFile << "Tamura front door (sunset) - BG08B";
		break;
	case 0x1B:
		OutFile << "Fountain (day) - BG09A";
		break;
	case 0x1C:
		OutFile << "Fountain (sunset) - BG09B";
		break;
	case 0x1D:
		OutFile << "Path with students (day) - BG10A";
		break;
	case 0x1E:
		OutFile << "Path without students (day) - BG10E";
		break;
	case 0x1F:
		OutFile << "Classroom1 with students (day) - BG11A";
		break;
	case 0x20:
		OutFile << "Classroom2 with students (day) - BG12A";
		break;
	case 0x21:
		OutFile << "Classroom2 without students (day) - BG12E";
		break;
	case 0x22:
		OutFile << "School hallway (day) - BG13A";
		break;
	case 0x23:
		OutFile << "Outside Gym shed (day) - BG14A";
		break;
	case 0x24:
		OutFile << "Club hallway (day) - BG15A";
		break;
	case 0x25:
		OutFile << "Shoe lockers (day) - BG16A";
		break;
	case 0x26:
		OutFile << "Residential road (day) - BG17A";
		break;
	case 0x27:
		OutFile << "Residential road (sunset) - BG17B";
		break;
	case 0x28:
		OutFile << "Gokou front door (day) - BG18A";
		break;
	case 0x29:
		OutFile << "Gokou front door (sunset) - BG18B";
		break;
	case 0x2A:
		OutFile << "Ruri's room (day) - BG19A";
		break;
	case 0x2B:
		OutFile << "Ruri's room (sunset) - BG19A";
		break;
	case 0x2C:
		OutFile << "Akihabara street1 (day) - BG20A";
		break;
	case 0x2D:
		OutFile << "Akihabara street1 (sunset) - BG20B";
		break;
	case 0x2E:
		OutFile << "Akihabara street2 (day) - BG21A";
		break;
	case 0x2F:
		OutFile << "Akihabara street2 (sunset) - BG21B";
		break;
	case 0x30:
		OutFile << "Cafe (day) - BG22A";
		break;
	case 0x31:
		OutFile << "Outside arcade (day) - BG23A";
		break;
	case 0x32:
		OutFile << "Outside arcade (sunset) - BG23B";
		break;
	case 0x33:
		OutFile << "Outside arcade (tournament) - BG23E";
		break;
	case 0x34:
		OutFile << "Shopping district street (day) - BG24A";
		break;
	case 0x35:
		OutFile << "Shopping district street (sunset) - BG24B";
		break;
	case 0x36:
		OutFile << "Outside Comifes (day) - BG25A";
		break;
	case 0x37:
		OutFile << "Outside Comifes (sunset) - BG25B";
		break;
	case 0x38:
		OutFile << "Comifes vendors - BG26A";
		break;
	case 0x39:
		OutFile << "Comifes stage - BG27A";
		break;
	case 0x3A:
		OutFile << "Table and boxes - BG28A";
		break;
	case 0x3B:
		OutFile << "Station - BG29A";
		break;
	case 0x3C:
		OutFile << "Kyoto shopping district - BG30A";
		break;
	case 0x3D:
		OutFile << "Kyoto temple - BG31A";
		break;
	case 0x3E:
		OutFile << "Kyoto hotel lobby - BG32C";
		break;
	case 0x3F:
		OutFile << "Kyoto hotel entrance (day) - BG33A";
		break;
	case 0x40:
		OutFile << "Kyoto hotel entrance (sunset) - BG33B";
		break;
	case 0x41:
		OutFile << "Clouds (day) - BG34A";
		break;
	case 0x42:
		OutFile << "Clouds (sunset) - BG34B";
		break;
	case 0x43:
		OutFile << "Clouds (night) - BG34C";
		break;
	case 0x44:
		OutFile << "Shopping mall - BG35A";
		break;
	case 0x45:
		OutFile << "Bench ferris wheel - BG36A";
		break;
	case 0x46:
		OutFile << "Doujinshi shop - BG37A";
		break;
	case 0x47:
		OutFile << "Kirino's door (day) - BG38A";
		break;
	case 0x48:
		OutFile << "Kirino's door (sunset) - BG38B";
		break;
	case 0x49:
		OutFile << "Kirino's door (?) - BG38C";
		break;
	case 0x4A:
		OutFile << "Kirino's door (night) - BG38D";
		break;
	case -1:
		OutFile << "Blank background";
		break;
	default:
		OutFile << "Unknown BG " << int(BG);
		break;
	}
}

void WriteCharacterName(char Character, ofstream& OutFile)
{
	switch (Character)
	{
		case 0x00:
			OutFile << "00 Character?";
			break;
		case 0x01:	
			OutFile << "? - AK_1L.gim";
			break;
		case 0x02:	
			OutFile << "? - AK_2C.gim";
			break;
		case 0x03:	
			OutFile << "? - AK_2L.gim";
			break;
		case 0x04:	
			OutFile << "Ayase - AY_1C.gim";
			break;
		case 0x05:	
			OutFile << "Ayase - AY_1L.gim";
			break;
		case 0x06:	
			OutFile << "Ayase - AY_1R.gim";
			break;
		case 0x07:	
			OutFile << "Ayase - AY_2C.gim";
			break;
		case 0x08:	
			OutFile << "Ayase - AY_2L.gim";
			break;
		case 0x09:	
			OutFile << "Ayase - AY_2R.gim";
			break;
		case 0x0A:	
			OutFile << "Ayase - AY_5C.gim";
			break;
		case 0x0B:	
			OutFile << "Ayase - AY_5L.gim";
			break;
		case 0x0C:	
			OutFile << "Ayase - AY_5R.gim";
			break;
		case 0x0D:	
			OutFile << "Ayase - AY_6C.gim";
			break;
		case 0x0E:	
			OutFile << "Ayase - AY_6L.gim";
			break;
		case 0x0F:	
			OutFile << "Ayase - AY_6R.gim";
			break;
		case 0x10:	
			OutFile << "Daisuke - DA_1C.gim";
			break;
		case 0x11:	
			OutFile << "? - KA_1C.gim";
			break;
		case 0x12:	
			OutFile << "? - KA_1R.gim";
			break;
		case 0x13:	
			OutFile << "? - KA_2C.gim";
			break;
		case 0x14:	
			OutFile << "? - KA_2R.gim";
			break;
		case 0x15:	
			OutFile << "? - KA_3C.gim";
			break;
		case 0x16:	
			OutFile << "Kirino - KI_1C.gim";
			break;
		case 0x17:	
			OutFile << "Kirino - KI_1L.gim";
			break;
		case 0x18:	
			OutFile << "Kirino - KI_1R.gim";
			break;
		case 0x19:	
			OutFile << "Kirino - KI_2C.gim";
			break;
		case 0x1A:	
			OutFile << "Kirino - KI_2L.gim";
			break;
		case 0x1B:	
			OutFile << "Kirino - KI_2R.gim";
			break;
		case 0x1C:	
			OutFile << "Kirino - KI_3C.gim";
			break;
		case 0x1D:	
			OutFile << "Kirino - KI_3L.gim";
			break;
		case 0x1E:	
			OutFile << "Kirino - KI_3R.gim";
			break;
		case 0x1F:	
			OutFile << "Kuroneko - KU_1C.gim";
			break;
		case 0x20:	
			OutFile << "Kuroneko - KU_1L.gim";
			break;
		case 0x21:	
			OutFile << "Kuroneko - KU_1R.gim";
			break;
		case 0x22:	
			OutFile << "Kuroneko - KU_2C.gim";
			break;
		case 0x23:	
			OutFile << "Kuroneko - KU_2L.gim";
			break;
		case 0x24:	
			OutFile << "Kuroneko - KU_2R.gim";
			break;
		case 0x25:	
			OutFile << "Manami - MA_1C.gim";
			break;
		case 0x26:	
			OutFile << "Manami - MA_1L.gim";
			break;
		case 0x27:	
			OutFile << "Manami - MA_1R.gim";
			break;
		case 0x28:	
			OutFile << "Manami - MA_2C.gim";
			break;
		case 0x29:	
			OutFile << "Manami - MA_2L.gim";
			break;
		case 0x2A:	
			OutFile << "Manami - MA_2R.gim";
			break;
		case 0x2B:	
			OutFile << "Rock - RO_1C.gim";
			break;
		case 0x2C:	
			OutFile << "Rock - RO_1R.gim";
			break;
		case 0x2D:	
			OutFile << "Saori - SA_1C.gim";
			break;
		case 0x2E:	
			OutFile << "Saori - SA_1L.gim";
			break;
		case 0x2F:	
			OutFile << "Saori - SA_1R.gim";
			break;
		case 0x30:	
			OutFile << "Saori - SA_2L.gim";
			break;
		case 0x31:	
			OutFile << "? - YO_1C.gim";
			break;
		default:
			OutFile << "Unknown character " << (int)Character;
			break;
	}
}

void WriteCG(char CG, ofstream& OutFile)
{
	switch (CG)
	{

		case 0x00:
			OutFile << "CG001A";
			break;
		case 0x01:	
			OutFile << "CG002A";
			break;
		case 0x02:	
			OutFile << "CG002B";
			break;
		case 0x03:	
			OutFile << "CG003A";
			break;
		case 0x04:	
			OutFile << "CG003B";
			break;
		case 0x05:	
			OutFile << "CG004A";
			break;
		case 0x06:	
			OutFile << "CG005A";
			break;
		case 0x07:	
			OutFile << "CG006A";
			break;
		case 0x08:	
			OutFile << "CG007A";
			break;
		case 0x09:	
			OutFile << "CG008A";
			break;
		case 0x0A:	
			OutFile << "CG009A";
			break;
		case 0x0B:	
			OutFile << "CG010A";
			break;
		case 0x0C:	
			OutFile << "CG010B";
			break;
		case 0x0D:	
			OutFile << "CG011A";
			break;
		case 0x0E:	
			OutFile << "CG011B";
			break;
		case 0x0F:	
			OutFile << "CG012A";
			break;
		case 0x10:	
			OutFile << "CG012B";
			break;
		case 0x11:	
			OutFile << "CG013A";
			break;
		case 0x12:	
			OutFile << "CG013B";
			break;
		case 0x13:	
			OutFile << "CG014A";
			break;
		case 0x14:	
			OutFile << "CG014B";
			break;
		case 0x15:	
			OutFile << "CG014C";
			break;
		case 0x16:	
			OutFile << "CG014D";
			break;
		case 0x17:	
			OutFile << "CG015A";
			break;
		case 0x18:	
			OutFile << "CG015B";
			break;
		case 0x19:	
			OutFile << "CG015C";
			break;
		case 0x1A:	
			OutFile << "CG016A";
			break;
		case 0x1B:	
			OutFile << "CG016B";
			break;
		case 0x1C:	
			OutFile << "CG017A";
			break;
		case 0x1D:	
			OutFile << "CG017B";
			break;
		case 0x1E:	
			OutFile << "CG018A";
			break;
		case 0x1F:	
			OutFile << "CG018B";
			break;
		case 0x20:	
			OutFile << "CG019A";
			break;
		case 0x21:	
			OutFile << "CG019B";
			break;
		case 0x22:	
			OutFile << "CG019C";
			break;
		case 0x23:	
			OutFile << "CG019D";
			break;
		case 0x24:	
			OutFile << "CG020A";
			break;
		case 0x25:	
			OutFile << "CG020B";
			break;
		case 0x26:	
			OutFile << "CG020C";
			break;
		case 0x27:	
			OutFile << "CG021A";
			break;
		case 0x28:	
			OutFile << "CG022A";
			break;
		case 0x29:	
			OutFile << "CG022B";
			break;
		case 0x2A:	
			OutFile << "CG022C";
			break;
		case 0x2B:	
			OutFile << "CG023A";
			break;
		case 0x2C:	
			OutFile << "CG023B";
			break;
		case 0x2D:	
			OutFile << "CG024A";
			break;
		case 0x2E:	
			OutFile << "CG024B";
			break;
		case 0x2F:	
			OutFile << "CG024C";
			break;
		case 0x30:	
			OutFile << "CG025A";
			break;
		case 0x31:	
			OutFile << "CG025B";
			break;
		case 0x32:	
			OutFile << "CG025C";
			break;
		case 0x33:	
			OutFile << "CG026A";
			break;
		case 0x34:	
			OutFile << "CG027A";
			break;
		case 0x35:	
			OutFile << "CG027B";
			break;
		case 0x36:	
			OutFile << "CG028A";
			break;
		case 0x37:	
			OutFile << "CG028B";
			break;
		case 0x38:	
			OutFile << "CG029A";
			break;
		case 0x39:	
			OutFile << "CG029B";
			break;
		case 0x3A:	
			OutFile << "CG029C";
			break;
		case 0x3B:	
			OutFile << "CG030A";
			break;
		case 0x3C:	
			OutFile << "CG030B";
			break;
		case 0x3D:	
			OutFile << "CG030C";
			break;
		case 0x3E:	
			OutFile << "CG030D";
			break;
		case 0x3F:	
			OutFile << "CG031A";
			break;
		case 0x40:	
			OutFile << "CG032A";
			break;
		case 0x41:	
			OutFile << "CG033A";
			break;
		case 0x42:	
			OutFile << "CG034A";
			break;
		case 0x43:	
			OutFile << "CG035A";
			break;
		case 0x44:	
			OutFile << "CG036A";
			break;
		case 0x45:	
			OutFile << "CG036B";
			break;
		case 0x46:	
			OutFile << "CG037A";
			break;
		case 0x47:	
			OutFile << "CG037B";
			break;
		case 0x48:	
			OutFile << "CG037C";
			break;
		case 0x49:	
			OutFile << "CG038A";
			break;
		case 0x4A:	
			OutFile << "CG039A";
			break;
		case 0x4B:	
			OutFile << "CG039B";
			break;
		case 0x4C:	
			OutFile << "CG039C";
			break;
		case 0x4D:	
			OutFile << "CG040A";
			break;
		case 0x4E:	
			OutFile << "CG041A";
			break;
		case 0x4F:	
			OutFile << "CG042A";
			break;
		case 0x50:	
			OutFile << "CG043A";
			break;


		case -1:
			OutFile << "Blank CG";
			break;

		default:
			OutFile << "Unknown CG " << (int)CG;
			break;
	}
}

void WriteORE(char ORE, ofstream& OutFile)
{
	switch (ORE)
	{

		case 0x00:
			OutFile << "1 - Siscaly Tournament";
			break;
		case 0x01:	
			OutFile << "2 - Tournament goods";
			break;
		case 0x02:	
			OutFile << "3 - National class";
			break;
		case 0x03:	
			OutFile << "4 - Keep calm!";
			break;
		case 0x04:	
			OutFile << "5";
			break;
		case 0x05:	
			OutFile << "6";
			break;
		case 0x06:	
			OutFile << "7";
			break;
		case 0x07:	
			OutFile << "8";
			break;
		case 0x08:	
			OutFile << "9";
			break;
		case 0x09:	
			OutFile << "10";
			break;
		case 0x0A:	
			OutFile << "11";
			break;
		case 0x0B:	
			OutFile << "12";
			break;
		case 0x0C:	
			OutFile << "13";
			break;
		case 0x0D:	
			OutFile << "14";
			break;
		case 0x0E:	
			OutFile << "15";
			break;
		case 0x0F:	
			OutFile << "16";
			break;
		case 0x10:	
			OutFile << "17";
			break;
		case 0x11:	
			OutFile << "18";
			break;
		case 0x12:	
			OutFile << "19";
			break;
		case 0x13:	
			OutFile << "20";
			break;
		case 0x14:	
			OutFile << "21";
			break;
		case 0x15:	
			OutFile << "22";
			break;
		case 0x16:	
			OutFile << "23";
			break;
		case 0x17:	
			OutFile << "24";
			break;
		case 0x18:	
			OutFile << "25";
			break;
		case 0x19:	
			OutFile << "26";
			break;
		case 0x1A:	
			OutFile << "27";
			break;
		case 0x1B:	
			OutFile << "28";
			break;
		case 0x1C:	
			OutFile << "29";
			break;
		case 0x1D:	
			OutFile << "30";
			break;
		case 0x1E:	
			OutFile << "31";
			break;
		case 0x1F:	
			OutFile << "32";
			break;
		case 0x20:	
			OutFile << "33";
			break;
		case 0x21:	
			OutFile << "34";
			break;
		case 0x22:	
			OutFile << "35";
			break;
		case 0x23:	
			OutFile << "36";
			break;
		case 0x24:	
			OutFile << "37";
			break;
		case 0x25:	
			OutFile << "38";
			break;
		case 0x26:	
			OutFile << "39";
			break;
		case 0x27:	
			OutFile << "40";
			break;
		case 0x28:	
			OutFile << "41";
			break;
		case 0x29:	
			OutFile << "42";
			break;
		case 0x2A:	
			OutFile << "43";
			break;
		case 0x2B:	
			OutFile << "44";
			break;
		case 0x2C:	
			OutFile << "45";
			break;
		case 0x2D:	
			OutFile << "46";
			break;
		case 0x2E:	
			OutFile << "47";
			break;
		case 0x2F:	
			OutFile << "48";
			break;
		case 0x30:	
			OutFile << "49";
			break;
		case 0x31:	
			OutFile << "50";
			break;
		case 0x32:	
			OutFile << "51";
			break;
		case 0x33:	
			OutFile << "52";
			break;
		case 0x34:	
			OutFile << "53";
			break;
		case 0x35:	
			OutFile << "54";
			break;
		case 0x36:	
			OutFile << "55";
			break;
		case 0x37:	
			OutFile << "56";
			break;
		case 0x38:	
			OutFile << "57";
			break;
		case 0x39:	
			OutFile << "58";
			break;
		case 0x3A:	
			OutFile << "59";
			break;
		case 0x3B:	
			OutFile << "60";
			break;
		case 0x3C:	
			OutFile << "61";
			break;
		case 0x3D:	
			OutFile << "62 - other side";
			break;
		case 0x3E:	
			OutFile << "63";
			break;
		case 0x3F:	
			OutFile << "64 - Ore no Imouto ga Konna ni Kawaii Wake ga Nai";
			break;
		case -1:
			OutFile << "Urgent evasion";
			break;
		default:
			OutFile << "Unknown ORE " << (int)ORE;
			break;
	}
}

void WriteScript(short Script, ofstream& OutFile)
{
		switch (Script)
		{
		case 0x00:
			OutFile << "AASTARTPOINT.txt";
			break;
		case 0x01:
			OutFile << "AKYO_0000A.txt";
			break;
		case 0x02:
			OutFile << "AKYO_0010A.txt";
			break;
		case 0x03:
			OutFile << "AKYO_0020T.txt";
			break;
		case 0x04:
			OutFile << "AKYO_0025A.txt";
			break;
		case 0x05:
			OutFile << "AKYO_0030A.txt";
			break;
		case 0x06:
			OutFile << "AKYO_0032T.txt";
			break;
		case 0x07:
			OutFile << "AKYO_0033A.txt";
			break;
		case 0x08:
			OutFile << "AKYO_0034A.txt";
			break;
		case 0x09:
			OutFile << "AKYO_0035A.txt";
			break;
		case 0x0a:
			OutFile << "AKYO_0036T.txt";
			break;
		case 0x0b:
			OutFile << "AKYO_0037A.txt";
			break;
		case 0x0c:
			OutFile << "AKYO_0038T.txt";
			break;
		case 0x0d:
			OutFile << "AKYO_0039A.txt";
			break;
		case 0x0e:
			OutFile << "AKYO_0040A.txt";
			break;
		case 0x0f:
			OutFile << "AKYO_0043T.txt";
			break;
		case 0x10:
			OutFile << "AKYO_0045A.txt";
			break;
		case 0x11:
			OutFile << "AKYO_0047A.txt";
			break;
		case 0x12:
			OutFile << "AKYO_0048A.txt";
			break;
		case 0x13:
			OutFile << "AKYO_0051A.txt";
			break;
		case 0x14:
			OutFile << "AKYO_0054A.txt";
			break;
		case 0x15:
			OutFile << "AKYO_0056T.txt";
			break;
		case 0x16:
			OutFile << "AKYO_0058A.txt";
			break;
		case 0x17:
			OutFile << "AKYO_0061A.txt";
			break;
		case 0x18:
			OutFile << "AKYO_0062A.txt";
			break;
		case 0x19:
			OutFile << "AKYO_0066A.txt";
			break;
		case 0x1a:
			OutFile << "AKYO_0068A.txt";
			break;
		case 0x1b:
			OutFile << "AKYO_0070T.txt";
			break;
		case 0x1c:
			OutFile << "AKYO_0072A.txt";
			break;
		case 0x1d:
			OutFile << "AKYO_0074A.txt";
			break;
		case 0x1e:
			OutFile << "AKYO_0078T.txt";
			break;
		case 0x1f:
			OutFile << "AKYO_0080A.txt";
			break;
		case 0x20:
			OutFile << "AKYO_0081A.txt";
			break;
		case 0x21:
			OutFile << "AKYO_0082T.txt";
			break;
		case 0x22:
			OutFile << "AKYO_0083A.txt";
			break;
		case 0x23:
			OutFile << "AKYO_0084A.txt";
			break;
		case 0x24:
			OutFile << "AKYO_0086A.txt";
			break;
		case 0x25:
			OutFile << "AKYO_0088A.txt";
			break;
		case 0x26:
			OutFile << "AKYO_0090A.txt";
			break;
		case 0x27:
			OutFile << "AKYO_0134A.txt";
			break;
		case 0x28:
			OutFile << "AKYO_0135A.txt";
			break;
		case 0x29:
			OutFile << "AKYO_0136T.txt";
			break;
		case 0x2a:
			OutFile << "AKYO_0137A.txt";
			break;
		case 0x2b:
			OutFile << "AKYO_0138T.txt";
			break;
		case 0x2c:
			OutFile << "AKYO_0139A.txt";
			break;
		case 0x2d:
			OutFile << "AKYO_0140A.txt";
			break;
		case 0x2e:
			OutFile << "AKYO_0143T.txt";
			break;
		case 0x2f:
			OutFile << "AKYO_0145A.txt";
			break;
		case 0x30:
			OutFile << "AKYO_0160A.txt";
			break;
		case 0x31:
			OutFile << "AKYO_0170A.txt";
			break;
		case 0x32:
			OutFile << "AKYO_0183A.txt";
			break;
		case 0x33:
			OutFile << "AKYO_0200A.txt";
			break;
		case 0x34:
			OutFile << "BKIR_0000A.txt";
			break;
		case 0x35:
			OutFile << "BKIR_0002G.txt";
			break;
		case 0x36:
			OutFile << "BKIR_0003A.txt";
			break;
		case 0x37:
			OutFile << "BKIR_0004T.txt";
			break;
		case 0x38:
			OutFile << "BKIR_0005A.txt";
			break;
		case 0x39:
			OutFile << "BKIR_0008A.txt";
			break;
		case 0x3a:
			OutFile << "BKIR_0010A.txt";
			break;
		case 0x3b:
			OutFile << "BKIR_0013T.txt";
			break;
		case 0x3c:
			OutFile << "BKIR_0015A.txt";
			break;
		case 0x3d:
			OutFile << "BKIR_0020T.txt";
			break;
		case 0x3e:
			OutFile << "BKIR_0023A.txt";
			break;
		case 0x3f:
			OutFile << "BKIR_0025T.txt";
			break;
		case 0x40:
			OutFile << "BKIR_0027A.txt";
			break;
		case 0x41:
			OutFile << "BKIR_0028G.txt";
			break;
		case 0x42:
			OutFile << "BKIR_0029A.txt";
			break;
		case 0x43:
			OutFile << "BKIR_0030A.txt";
			break;
		case 0x44:
			OutFile << "BKIR_0031A.txt";
			break;
		case 0x45:
			OutFile << "BKIR_0033T.txt";
			break;
		case 0x46:
			OutFile << "BKIR_0035A.txt";
			break;
		case 0x47:
			OutFile << "BKIR_0045A.txt";
			break;
		case 0x48:
			OutFile << "BKIR_0048T.txt";
			break;
		case 0x49:
			OutFile << "BKIR_0050A.txt";
			break;
		case 0x4a:
			OutFile << "BKIR_0053G.txt";
			break;
		case 0x4b:
			OutFile << "BKIR_0055A.txt";
			break;
		case 0x4c:
			OutFile << "BKIR_0058A.txt";
			break;
		case 0x4d:
			OutFile << "BKIR_0060A.txt";
			break;
		case 0x4e:
			OutFile << "BKIR_0063T.txt";
			break;
		case 0x4f:
			OutFile << "BKIR_0065A.txt";
			break;
		case 0x50:
			OutFile << "BKIR_0070A.txt";
			break;
		case 0x51:
			OutFile << "BKIR_0071G.txt";
			break;
		case 0x52:
			OutFile << "BKIR_0072A.txt";
			break;
		case 0x53:
			OutFile << "BKIR_0075A.txt";
			break;
		case 0x54:
			OutFile << "BKIR_0083T.txt";
			break;
		case 0x55:
			OutFile << "BKIR_0085G.txt";
			break;
		case 0x56:
			OutFile << "BKIR_0090A.txt";
			break;
		case 0x57:
			OutFile << "BKIR_0100A.txt";
			break;
		case 0x58:
			OutFile << "BKIR_0101G.txt";
			break;
		case 0x59:
			OutFile << "BKIR_0102A.txt";
			break;
		case 0x5a:
			OutFile << "BKIR_0103T.txt";
			break;
		case 0x5b:
			OutFile << "BKIR_0105A.txt";
			break;
		case 0x5c:
			OutFile << "BKIR_0106A.txt";
			break;
		case 0x5d:
			OutFile << "BKIR_0107A.txt";
			break;
		case 0x5e:
			OutFile << "BKIR_0109E.txt";
			break;
		case 0x5f:
			OutFile << "BKIR_0110A.txt";
			break;
		case 0x60:
			OutFile << "BKIR_0113G.txt";
			break;
		case 0x61:
			OutFile << "BKIR_0115A.txt";
			break;
		case 0x62:
			OutFile << "BKIR_0120A.txt";
			break;
		case 0x63:
			OutFile << "BKIR_0123T.txt";
			break;
		case 0x64:
			OutFile << "BKIR_0125A.txt";
			break;
		case 0x65:
			OutFile << "BKIR_0130G.txt";
			break;
		case 0x66:
			OutFile << "BKIR_0140E.txt";
			break;
		case 0x67:
			OutFile << "BKIR_0150E.txt";
			break;
		case 0x68:
			OutFile << "BKIR_0160E.txt";
			break;
		case 0x69:
			OutFile << "CKUR_0000A.txt";
			break;
		case 0x6a:
			OutFile << "CKUR_0001A.txt";
			break;
		case 0x6b:
			OutFile << "CKUR_0010A.txt";
			break;
		case 0x6c:
			OutFile << "CKUR_0020A.txt";
			break;
		case 0x6d:
			OutFile << "CKUR_0030A.txt";
			break;
		case 0x6e:
			OutFile << "CKUR_0035T.txt";
			break;
		case 0x6f:
			OutFile << "CKUR_0038A.txt";
			break;
		case 0x70:
			OutFile << "CKUR_0040A.txt";
			break;
		case 0x71:
			OutFile << "CKUR_0043T.txt";
			break;
		case 0x72:
			OutFile << "CKUR_0045A.txt";
			break;
		case 0x73:
			OutFile << "CKUR_0050A.txt";
			break;
		case 0x74:
			OutFile << "CKUR_0053A.txt";
			break;
		case 0x75:
			OutFile << "CKUR_0055T.txt";
			break;
		case 0x76:
			OutFile << "CKUR_0058A.txt";
			break;
		case 0x77:
			OutFile << "CKUR_0060A.txt";
			break;
		case 0x78:
			OutFile << "CKUR_0061A.txt";
			break;
		case 0x79:
			OutFile << "CKUR_0065G.txt";
			break;
		case 0x7a:
			OutFile << "CKUR_0067T.txt";
			break;
		case 0x7b:
			OutFile << "CKUR_0080A.txt";
			break;
		case 0x7c:
			OutFile << "CKUR_0085G.txt";
			break;
		case 0x7d:
			OutFile << "CKUR_0090A.txt";
			break;
		case 0x7e:
			OutFile << "CKUR_0093T.txt";
			break;
		case 0x7f:
			OutFile << "CKUR_0095A.txt";
			break;
		case 0x80:
			OutFile << "CKUR_0098A.txt";
			break;
		case 0x81:
			OutFile << "CKUR_0100T.txt";
			break;
		case 0x82:
			OutFile << "CKUR_0101A.txt";
			break;
		case 0x83:
			OutFile << "CKUR_0102A.txt";
			break;
		case 0x84:
			OutFile << "CKUR_0103A.txt";
			break;
		case 0x85:
			OutFile << "CKUR_0104A.txt";
			break;
		case 0x86:
			OutFile << "CKUR_0105T.txt";
			break;
		case 0x87:
			OutFile << "CKUR_0106G.txt";
			break;
		case 0x88:
			OutFile << "CKUR_0107A.txt";
			break;
		case 0x89:
			OutFile << "CKUR_0108T.txt";
			break;
		case 0x8a:
			OutFile << "CKUR_0109A.txt";
			break;
		case 0x8b:
			OutFile << "CKUR_0110A.txt";
			break;
		case 0x8c:
			OutFile << "CKUR_0120A.txt";
			break;
		case 0x8d:
			OutFile << "CKUR_0125T.txt";
			break;
		case 0x8e:
			OutFile << "CKUR_0130A.txt";
			break;
		case 0x8f:
			OutFile << "CKUR_0135A.txt";
			break;
		case 0x90:
			OutFile << "CKUR_0140A.txt";
			break;
		case 0x91:
			OutFile << "CKUR_0150A.txt";
			break;
		case 0x92:
			OutFile << "CKUR_0160A.txt";
			break;
		case 0x93:
			OutFile << "CKUR_0165G.txt";
			break;
		case 0x94:
			OutFile << "CKUR_0168A.txt";
			break;
		case 0x95:
			OutFile << "CKUR_0170A.txt";
			break;
		case 0x96:
			OutFile << "CKUR_0175T.txt";
			break;
		case 0x97:
			OutFile << "CKUR_0178A.txt";
			break;
		case 0x98:
			OutFile << "CKUR_0179G.txt";
			break;
		case 0x99:
			OutFile << "CKUR_0180A.txt";
			break;
		case 0x9a:
			OutFile << "CKUR_0190E.txt";
			break;
		case 0x9b:
			OutFile << "CKUR_0195E.txt";
			break;
		case 0x9c:
			OutFile << "CKUR_0200E.txt";
			break;
		case 0x9d:
			OutFile << "CKUR_0210E.txt";
			break;
		case 0x9e:
			OutFile << "DSAO_0000A.txt";
			break;
		case 0x9f:
			OutFile << "DSAO_0003T.txt";
			break;
		case 0xa0:
			OutFile << "DSAO_0005A.txt";
			break;
		case 0xa1:
			OutFile << "DSAO_0007T.txt";
			break;
		case 0xa2:
			OutFile << "DSAO_0008A.txt";
			break;
		case 0xa3:
			OutFile << "DSAO_0009G.txt";
			break;
		case 0xa4:
			OutFile << "DSAO_0010A.txt";
			break;
		case 0xa5:
			OutFile << "DSAO_0015A.txt";
			break;
		case 0xa6:
			OutFile << "DSAO_0020A.txt";
			break;
		case 0xa7:
			OutFile << "DSAO_0023A.txt";
			break;
		case 0xa8:
			OutFile << "DSAO_0024A.txt";
			break;
		case 0xa9:
			OutFile << "DSAO_0025G.txt";
			break;
		case 0xaa:
			OutFile << "DSAO_0026T.txt";
			break;
		case 0xab:
			OutFile << "DSAO_0029A.txt";
			break;
		case 0xac:
			OutFile << "DSAO_0030G.txt";
			break;
		case 0xad:
			OutFile << "DSAO_0037A.txt";
			break;
		case 0xae:
			OutFile << "DSAO_0038T.txt";
			break;
		case 0xaf:
			OutFile << "DSAO_0039A.txt";
			break;
		case 0xb0:
			OutFile << "DSAO_0040A.txt";
			break;
		case 0xb1:
			OutFile << "DSAO_0045A.txt";
			break;
		case 0xb2:
			OutFile << "DSAO_0053T.txt";
			break;
		case 0xb3:
			OutFile << "DSAO_0055A.txt";
			break;
		case 0xb4:
			OutFile << "DSAO_0058T.txt";
			break;
		case 0xb5:
			OutFile << "DSAO_0060A.txt";
			break;
		case 0xb6:
			OutFile << "DSAO_0063T.txt";
			break;
		case 0xb7:
			OutFile << "DSAO_0064G.txt";
			break;
		case 0xb8:
			OutFile << "DSAO_0065A.txt";
			break;
		case 0xb9:
			OutFile << "DSAO_0067A.txt";
			break;
		case 0xba:
			OutFile << "DSAO_0070A.txt";
			break;
		case 0xbb:
			OutFile << "DSAO_0072T.txt";
			break;
		case 0xbc:
			OutFile << "DSAO_0073A.txt";
			break;
		case 0xbd:
			OutFile << "DSAO_0075T.txt";
			break;
		case 0xbe:
			OutFile << "DSAO_0078A.txt";
			break;
		case 0xbf:
			OutFile << "DSAO_0080T.txt";
			break;
		case 0xc0:
			OutFile << "DSAO_0087A.txt";
			break;
		case 0xc1:
			OutFile << "DSAO_0088G.txt";
			break;
		case 0xc2:
			OutFile << "DSAO_0090E.txt";
			break;
		case 0xc3:
			OutFile << "DSAO_0100E.txt";
			break;
		case 0xc4:
			OutFile << "DSAO_0102E.txt";
			break;
		case 0xc5:
			OutFile << "DSAO_0105E.txt";
			break;
		case 0xc6:
			OutFile << "DSAO_0110E.txt";
			break;
		case 0xc7:
			OutFile << "EMAN_0000A.txt";
			break;
		case 0xc8:
			OutFile << "EMAN_0003A.txt";
			break;
		case 0xc9:
			OutFile << "EMAN_0005G.txt";
			break;
		case 0xca:
			OutFile << "EMAN_0006A.txt";
			break;
		case 0xcb:
			OutFile << "EMAN_0007T.txt";
			break;
		case 0xcc:
			OutFile << "EMAN_0008G.txt";
			break;
		case 0xcd:
			OutFile << "EMAN_0009A.txt";
			break;
		case 0xce:
			OutFile << "EMAN_0010A.txt";
			break;
		case 0xcf:
			OutFile << "EMAN_0012G.txt";
			break;
		case 0xd0:
			OutFile << "EMAN_0013T.txt";
			break;
		case 0xd1:
			OutFile << "EMAN_0015A.txt";
			break;
		case 0xd2:
			OutFile << "EMAN_0020A.txt";
			break;
		case 0xd3:
			OutFile << "EMAN_0030A.txt";
			break;
		case 0xd4:
			OutFile << "EMAN_0033T.txt";
			break;
		case 0xd5:
			OutFile << "EMAN_0035A.txt";
			break;
		case 0xd6:
			OutFile << "EMAN_0039A.txt";
			break;
		case 0xd7:
			OutFile << "EMAN_0040T.txt";
			break;
		case 0xd8:
			OutFile << "EMAN_0041A.txt";
			break;
		case 0xd9:
			OutFile << "EMAN_0042G.txt";
			break;
		case 0xda:
			OutFile << "EMAN_0043A.txt";
			break;
		case 0xdb:
			OutFile << "EMAN_0044T.txt";
			break;
		case 0xdc:
			OutFile << "EMAN_0045A.txt";
			break;
		case 0xdd:
			OutFile << "EMAN_0046A.txt";
			break;
		case 0xde:
			OutFile << "EMAN_0047A.txt";
			break;
		case 0xdf:
			OutFile << "EMAN_0048A.txt";
			break;
		case 0xe0:
			OutFile << "EMAN_0049A.txt";
			break;
		case 0xe1:
			OutFile << "EMAN_0050G.txt";
			break;
		case 0xe2:
			OutFile << "EMAN_0052A.txt";
			break;
		case 0xe3:
			OutFile << "EMAN_0054A.txt";
			break;
		case 0xe4:
			OutFile << "EMAN_0056T.txt";
			break;
		case 0xe5:
			OutFile << "EMAN_0058A.txt";
			break;
		case 0xe6:
			OutFile << "EMAN_0060A.txt";
			break;
		case 0xe7:
			OutFile << "EMAN_0062A.txt";
			break;
		case 0xe8:
			OutFile << "EMAN_0064T.txt";
			break;
		case 0xe9:
			OutFile << "EMAN_0066A.txt";
			break;
		case 0xea:
			OutFile << "EMAN_0070E.txt";
			break;
		case 0xeb:
			OutFile << "EMAN_0080E.txt";
			break;
		case 0xec:
			OutFile << "EMAN_0090E.txt";
			break;
		case 0xed:
			OutFile << "FAYA_0000A.txt";
			break;
		case 0xee:
			OutFile << "FAYA_0001A.txt";
			break;
		case 0xef:
			OutFile << "FAYA_0002A.txt";
			break;
		case 0xf0:
			OutFile << "FAYA_0003A.txt";
			break;
		case 0xf1:
			OutFile << "FAYA_0004A.txt";
			break;
		case 0xf2:
			OutFile << "FAYA_0005A.txt";
			break;
		case 0xf3:
			OutFile << "FAYA_0010A.txt";
			break;
		case 0xf4:
			OutFile << "FAYA_0011T.txt";
			break;
		case 0xf5:
			OutFile << "FAYA_0012A.txt";
			break;
		case 0xf6:
			OutFile << "FAYA_0013A.txt";
			break;
		case 0xf7:
			OutFile << "FAYA_0014A.txt";
			break;
		case 0xf8:
			OutFile << "FAYA_0015T.txt";
			break;
		case 0xf9:
			OutFile << "FAYA_0016A.txt";
			break;
		case 0xfa:
			OutFile << "FAYA_0020A.txt";
			break;
		case 0xfb:
			OutFile << "FAYA_0030A.txt";
			break;
		case 0xfc:
			OutFile << "FAYA_0031A.txt";
			break;
		case 0xfd:
			OutFile << "FAYA_0032T.txt";
			break;
		case 0xfe:
			OutFile << "FAYA_0033A.txt";
			break;
		case 0xff:
			OutFile << "FAYA_0034T.txt";
			break;
		case 0x100:
			OutFile << "FAYA_0035A.txt";
			break;
		case 0x101:
			OutFile << "FAYA_0040A.txt";
			break;
		case 0x102:
			OutFile << "FAYA_0041A.txt";
			break;
		case 0x103:
			OutFile << "FAYA_0042A.txt";
			break;
		case 0x104:
			OutFile << "FAYA_0043T.txt";
			break;
		case 0x105:
			OutFile << "FAYA_0044A.txt";
			break;
		case 0x106:
			OutFile << "FAYA_0045A.txt";
			break;
		case 0x107:
			OutFile << "FAYA_0100E.txt";
			break;
		case 0x108:
			OutFile << "FAYA_0110E.txt";
			break;
		case 0x109:
			OutFile << "FAYA_0120E.txt";
			break;
		case 0x10a:
			OutFile << "GIFG_0000A.txt";
			break;
		case 0x10b:
			OutFile << "GIFG_0005T.txt";
			break;
		case 0x10c:
			OutFile << "GIFG_0008A.txt";
			break;
		case 0x10d:
			OutFile << "GIFG_0009E.txt";
			break;
		case 0x10e:
			OutFile << "GIFG_0010A.txt";
			break;
		case 0x10f:
			OutFile << "GIFG_0011T.txt";
			break;
		case 0x110:
			OutFile << "GIFG_0012A.txt";
			break;
		case 0x111:
			OutFile << "GIFG_0013E.txt";
			break;
		case 0x112:
			OutFile << "GIFG_0014A.txt";
			break;
		case 0x113:
			OutFile << "GIFG_0014T.txt";
			break;
		case 0x114:
			OutFile << "GIFG_0015E.txt";
			break;
		case 0x115:
			OutFile << "GIFG_0016A.txt";
			break;
		case 0x116:
			OutFile << "GIFG_0017T.txt";
			break;
		case 0x117:
			OutFile << "GIFG_0018E.txt";
			break;
		case 0x118:
			OutFile << "GIFG_0019A.txt";
			break;
		case 0x119:
			OutFile << "GIFG_0020A.txt";
			break;
		case 0x11a:
			OutFile << "GIFG_0030A.txt";
			break;
		case 0x11b:
			OutFile << "GIFG_0033T.txt";
			break;
		case 0x11c:
			OutFile << "GIFG_0035E.txt";
			break;
		case 0x11d:
			OutFile << "GIFG_0037A.txt";
			break;
		case 0x11e:
			OutFile << "GIFG_0038T.txt";
			break;
		case 0x11f:
			OutFile << "GIFG_0039E.txt";
			break;
		case 0x120:
			OutFile << "GIFG_0040A.txt";
			break;
		case 0x121:
			OutFile << "GIFG_0042A.txt";
			break;
		case 0x122:
			OutFile << "GIFG_0050E.txt";
			break;
		case 0x123:
			OutFile << "HKYM_0000E.txt";
			break;
		case 0x124:
			OutFile << "IYAN_0000E.txt";
			break;
		case 0x125:
			OutFile << "JROC_0000E.txt";
			break;
		case 0x126:
			OutFile << "KIFK_0000A.txt";
			break;
		case 0x127:
			OutFile << "KIFK_0001A.txt";
			break;
		case 0x128:
			OutFile << "KIFK_0002A.txt";
			break;
		case 0x129:
			OutFile << "KIFK_0003E.txt";
			break;
		case 0x12a:
			OutFile << "LAKA_0000.txt";
			break;
		case 0x12b:
			OutFile << "MGIM_0000.txt";
			break;
		default:
			OutFile << "Unknown script " << Script;
			break;
	}

}

void WriteBGM(char BGM, ofstream& OutFile)
{
	switch (BGM)
	{
		case 0x00:
			OutFile << "1 - oRE No IMoUTo";
			break;
		case 0x01:	
			OutFile << "2 - GOOD MORNING";
			break;
		case 0x02:	
			OutFile << "3 - Immediate Approach";
			break;
		case 0x03:	
			OutFile << "4 - SOFT BREEZEEEEEE";
			break;
		case 0x04:	
			OutFile << "5 - Merry Go Round";
			break;
		case 0x05:	
			OutFile << "6 - Roller Coaster";
			break;
		case 0x06:	
			OutFile << "7 - downpour";
			break;
		case 0x07:	
			OutFile << "8 - Ferris wheel";
			break;
		case 0x08:	
			OutFile << "9 - STONiSHMENT HOUSE";
			break;
		case 0x09:	
			OutFile << "10 - After a storm comes a calm";
			break;
		case 0x0A:	
			OutFile << "11 - street performer";
			break;
		case 0x0B:	
			OutFile << "12 - Real Substantiality";
			break;
		case 0x0C:	
			OutFile << "13 - rendezvous";
			break;
		case 0x0D:	
			OutFile << "14 - AKiHABARA, CHiYODA-KU";
			break;
		case 0x0E:	
			OutFile << "15 - Super Approach!!";
			break;
		case 0x0F:	
			OutFile << "16 - DESTROY GAME STORY";
			break;
		case 0x10:	
			OutFile << "17 - SECRETX2";
			break;
		default:
			OutFile << "Unknown BGM " << BGM;
			break;
	}
}

void WriteCutin(char Cutin, ofstream& OutFile)
{
	switch (Cutin)
	{
		case 0x00:
			OutFile << "1 - Siscaly Tournament GET";
			break;
		case 0x01:	
			OutFile << "2 - Tournament goods GET";
			break;
		case 0x02:	
			OutFile << "3 - National class GET";
			break;
		case 0x03:	
			OutFile << "4 - Keep calm! GET";
			break;
		case 0x04:	
			OutFile << "5";
			break;
		case 0x05:	
			OutFile << "6";
			break;
		case 0x06:	
			OutFile << "7";
			break;
		case 0x07:	
			OutFile << "8";
			break;
		case 0x08:	
			OutFile << "9";
			break;
		case 0x09:	
			OutFile << "10";
			break;
		case 0x0A:	
			OutFile << "11";
			break;
		case 0x0B:	
			OutFile << "12";
			break;
		case 0x0C:	
			OutFile << "13";
			break;
		case 0x0D:	
			OutFile << "14";
			break;
		case 0x0E:	
			OutFile << "15";
			break;
		case 0x0F:	
			OutFile << "16";
			break;
		case 0x10:	
			OutFile << "17";
			break;
		case 0x11:	
			OutFile << "18";
			break;
		case 0x12:	
			OutFile << "19";
			break;
		case 0x13:	
			OutFile << "20";
			break;
		case 0x14:	
			OutFile << "21";
			break;
		case 0x15:	
			OutFile << "22";
			break;
		case 0x16:	
			OutFile << "23";
			break;
		case 0x17:	
			OutFile << "24";
			break;
		case 0x18:	
			OutFile << "25";
			break;
		case 0x19:	
			OutFile << "26";
			break;
		case 0x1A:	
			OutFile << "27";
			break;
		case 0x1B:	
			OutFile << "28";
			break;
		case 0x1C:	
			OutFile << "29";
			break;
		case 0x1D:	
			OutFile << "30";
			break;
		case 0x1E:	
			OutFile << "31";
			break;
		case 0x1F:	
			OutFile << "32";
			break;
		case 0x20:	
			OutFile << "33";
			break;
		case 0x21:	
			OutFile << "34";
			break;
		case 0x22:	
			OutFile << "35";
			break;
		case 0x23:	
			OutFile << "36";
			break;
		case 0x24:	
			OutFile << "37";
			break;
		case 0x25:	
			OutFile << "38";
			break;
		case 0x26:	
			OutFile << "39";
			break;
		case 0x27:	
			OutFile << "40";
			break;
		case 0x28:	
			OutFile << "41";
			break;
		case 0x29:	
			OutFile << "42";
			break;
		case 0x2A:	
			OutFile << "43";
			break;
		case 0x2B:	
			OutFile << "44";
			break;
		case 0x2C:	
			OutFile << "45";
			break;
		case 0x2D:	
			OutFile << "46";
			break;
		case 0x2E:	
			OutFile << "47";
			break;
		case 0x2F:	
			OutFile << "48";
			break;
		case 0x30:	
			OutFile << "49";
			break;
		case 0x31:	
			OutFile << "50";
			break;
		case 0x32:	
			OutFile << "51";
			break;
		case 0x33:	
			OutFile << "52";
			break;
		case 0x34:	
			OutFile << "53";
			break;
		case 0x35:	
			OutFile << "54";
			break;
		case 0x36:	
			OutFile << "55";
			break;
		case 0x37:	
			OutFile << "56";
			break;
		case 0x38:	
			OutFile << "57";
			break;
		case 0x39:	
			OutFile << "58";
			break;
		case 0x3A:	
			OutFile << "59";
			break;
		case 0x3B:	
			OutFile << "60";
			break;
		case 0x3C:	
			OutFile << "61";
			break;
		case 0x3D:	
			OutFile << "62 - other side GET";
			break;
		case 0x3E:	
			OutFile << "63";
			break;
		case 0x3F:	
			OutFile << "64 - Ore no Imouto ga Konna ni Kawaii Wake ga Nai GET";
			break;
		case 0x40:
			OutFile << "65 - cell phone";
			break;
		case 0x41:
			OutFile << "66 - computer";
			break;
		case 0x42:
			OutFile << "67 - two shot start";
			break;
		case 0x43:
			OutFile << "68 - two shot end";
			break;
		case 0x44:
			OutFile << "69 - urgent evasion!?";
			break;

		default:
			OutFile << "Unknown cutin " << (int)Cutin;
			break;
	}
}

void WriteSE(char SE, ofstream& OutFile)
{
		switch (SE)
		{
		case 0x00:
			OutFile << "SAY_003.hca";
			break;
		case 0x01:
			OutFile << "SE000.hca";
			break;
		case 0x02:
			OutFile << "SE001.hca";
			break;
		case 0x03:
			OutFile << "SE002.hca";
			break;
		case 0x04:
			OutFile << "SE003.hca";
			break;
		case 0x05:
			OutFile << "SE004.hca";
			break;
		case 0x06:
			OutFile << "SE005.hca";
			break;
		case 0x07:
			OutFile << "SE006.hca";
			break;
		case 0x08:
			OutFile << "SE007.hca";
			break;
		case 0x09:
			OutFile << "SE008.hca";
			break;
		case 0x0a:
			OutFile << "SE009.hca";
			break;
		case 0x0b:
			OutFile << "SE010.hca";
			break;
		case 0x0c:
			OutFile << "SE011.hca";
			break;
		case 0x0d:
			OutFile << "SE012.hca";
			break;
		case 0x0e:
			OutFile << "SE013.hca";
			break;
		case 0x0f:
			OutFile << "SE014.hca";
			break;
		case 0x10:
			OutFile << "SE015.hca";
			break;
		case 0x11:
			OutFile << "SE016.hca";
			break;
		case 0x12:
			OutFile << "SE017.hca";
			break;
		case 0x13:
			OutFile << "SE018.hca";
			break;
		case 0x14:
			OutFile << "SE019.hca";
			break;
		case 0x15:
			OutFile << "SE020.hca";
			break;
		case 0x16:
			OutFile << "SE021.hca";
			break;
		case 0x17:
			OutFile << "SE022.hca";
			break;
		case 0x18:
			OutFile << "SE023.hca";
			break;
		case 0x19:
			OutFile << "SE024.hca";
			break;
		case 0x1a:
			OutFile << "SE025.hca";
			break;
		case 0x1b:
			OutFile << "SE026.hca";
			break;
		case 0x1c:
			OutFile << "SE027.hca";
			break;
		case 0x1d:
			OutFile << "SE028.hca";
			break;
		case 0x1e:
			OutFile << "SE029.hca";
			break;
		case 0x1f:
			OutFile << "SE030.hca";
			break;
		case 0x20:
			OutFile << "SE031.hca";
			break;
		case 0x21:
			OutFile << "SE032.hca";
			break;
		case 0x22:
			OutFile << "SE033.hca";
			break;
		case 0x23:
			OutFile << "SE034.hca";
			break;
		case 0x24:
			OutFile << "SE035.hca";
			break;
		case 0x25:
			OutFile << "SE036.hca";
			break;
		case 0x26:
			OutFile << "SE037.hca";
			break;
		case 0x27:
			OutFile << "SE038.hca";
			break;
		case 0x28:
			OutFile << "SE039.hca";
			break;
		case 0x29:
			OutFile << "SE040.hca";
			break;
		case 0x2a:
			OutFile << "SE041.hca";
			break;
		case 0x2b:
			OutFile << "SE042.hca";
			break;
		case 0x2c:
			OutFile << "SE043.hca";
			break;
		case 0x2d:
			OutFile << "SE044.hca";
			break;
		case 0x2e:
			OutFile << "SE045.hca";
			break;
		case 0x2f:
			OutFile << "SE046.hca";
			break;
		case 0x30:
			OutFile << "SE047.hca";
			break;
		case 0x31:
			OutFile << "SE048.hca";
			break;
		case 0x32:
			OutFile << "SE049.hca";
			break;
		case 0x33:
			OutFile << "SE050.hca";
			break;
		case 0x34:
			OutFile << "SE051.hca";
			break;
		case 0x35:
			OutFile << "SE052.hca";
			break;
		case 0x36:
			OutFile << "SE053.hca";
			break;
		case 0x37:
			OutFile << "SE054.hca";
			break;
		case 0x38:
			OutFile << "SE055.hca";
			break;
		case 0x39:
			OutFile << "SE056.hca";
			break;
		case 0x3a:
			OutFile << "SE057.hca";
			break;
		case 0x3b:
			OutFile << "SE058.hca";
			break;
		case 0x3c:
			OutFile << "SE059.hca";
			break;
		case 0x3d:
			OutFile << "SE060.hca";
			break;
		case 0x3e:
			OutFile << "SE061.hca";
			break;
		case 0x3f:
			OutFile << "SE062.hca";
			break;
		case 0x40:
			OutFile << "SE063.hca";
			break;
		case 0x41:
			OutFile << "SE064.hca";
			break;
		case 0x42:
			OutFile << "SE065.hca";
			break;
		case 0x43:
			OutFile << "SE066.hca";
			break;
		case 0x44:
			OutFile << "SE067.hca";
			break;
		case 0x45:
			OutFile << "SE068.hca";
			break;
		case 0x46:
			OutFile << "SE069.hca";
			break;
		case 0x47:
			OutFile << "SE070.hca";
			break;
		case 0x48:
			OutFile << "SE071.hca";
			break;
		case 0x49:
			OutFile << "SE072.hca";
			break;
		case 0x4a:
			OutFile << "SE073.hca";
			break;
		case 0x4b:
			OutFile << "SE074.hca";
			break;
		case 0x4c:
			OutFile << "SE075.hca";
			break;
		case 0x4d:
			OutFile << "SE076.hca";
			break;
		case 0x4e:
			OutFile << "SE077.hca";
			break;
		case 0x4f:
			OutFile << "SE078.hca";
			break;
		case 0x50:
			OutFile << "SE079.hca";
			break;
		case 0x51:
			OutFile << "SE080.hca";
			break;
		case 0x52:
			OutFile << "SE081.hca";
			break;
		case 0x53:
			OutFile << "SE082.hca";
			break;
		case 0x54:
			OutFile << "SE083.hca";
			break;
		case 0x55:
			OutFile << "SE084.hca";
			break;
		case 0x56:
			OutFile << "SE085.hca";
			break;
		case 0x57:
			OutFile << "SE086.hca";
			break;
		case 0x58:
			OutFile << "SE087.hca";
			break;
		case 0x59:
			OutFile << "SE088.hca";
			break;
		case 0x5a:
			OutFile << "SE089.hca";
			break;
		case 0x5b:
			OutFile << "SE090.hca";
			break;
		case 0x5c:
			OutFile << "SE091.hca";
			break;
		case 0x5d:
			OutFile << "SE092.hca";
			break;
		case 0x5e:
			OutFile << "SE093.hca";
			break;
		case 0x5f:
			OutFile << "SE094.hca";
			break;
		case 0x60:
			OutFile << "SE095.hca";
			break;
		case 0x61:
			OutFile << "SE096.hca";
			break;
		case 0x62:
			OutFile << "SE097.hca";
			break;
		case 0x63:
			OutFile << "SE098.hca";
			break;
		case 0x64:
			OutFile << "SE099.hca";
			break;
		case 0x65:
			OutFile << "SE100.hca";
			break;
		case 0x66:
			OutFile << "SE101.hca";
			break;
		case 0x67:
			OutFile << "SE102.hca";
			break;
		case 0x68:
			OutFile << "SE103.hca";
			break;
		case 0x69:
			OutFile << "SE104.hca";
			break;
		case 0x6a:
			OutFile << "SE105.hca";
			break;
		case 0x6b:
			OutFile << "SE106.hca";
			break;
		case 0x6c:
			OutFile << "SE107.hca";
			break;
		case 0x6d:
			OutFile << "SE108.hca";
			break;
		case 0x6e:
			OutFile << "SE109.hca";
			break;
		case 0x6f:
			OutFile << "SE110.hca";
			break;
		case 0x70:
			OutFile << "SE111.hca";
			break;
		case 0x71:
			OutFile << "SE112.hca";
			break;
		case 0x72:
			OutFile << "SE113.hca";
			break;
		case 0x73:
			OutFile << "SE114.hca";
			break;
		case 0x74:
			OutFile << "SE115.hca";
			break;
		case 0x75:
			OutFile << "SE116.hca";
			break;
		case 0x76:
			OutFile << "SE118.hca";
			break;
		case 0x77:
			OutFile << "SE119.hca";
			break;
		case 0x78:
			OutFile << "SE120.hca";
			break;
		case 0x79:
			OutFile << "SE121.hca";
			break;
		case 0x7a:
			OutFile << "SE122.hca";
			break;
		case 0x7b:
			OutFile << "SE123.hca";
			break;
		case 0x7c:
			OutFile << "SE124.hca";
			break;
		case 0x7d:
			OutFile << "SE125.hca";
			break;
		case 0x7e:
			OutFile << "SE126.hca";
			break;
		case 0x7f:
			OutFile << "SE127.hca";
			break;
		case 0x80:
			OutFile << "SE128.hca";
			break;
		case 0x81:
			OutFile << "SE129.hca";
			break;
		case 0x82:
			OutFile << "SE130.hca";
			break;
		case 0x83:
			OutFile << "SE131.hca";
			break;
		case 0x84:
			OutFile << "SE132.hca";
			break;
		case 0x85:
			OutFile << "SE133.hca";
			break;
		case 0x86:
			OutFile << "SE134.hca";
			break;
		case 0x87:
			OutFile << "SE135.hca";
			break;
		case 0x88:
			OutFile << "SE136.hca";
			break;
		case 0x89:
			OutFile << "SE137.hca";
			break;
		case 0x8a:
			OutFile << "SE138.hca";
			break;
		case 0x8b:
			OutFile << "SE139.hca";
			break;
		case 0x8c:
			OutFile << "SE140.hca";
			break;
		case 0x8d:
			OutFile << "SE141.hca";
			break;
		case 0x8e:
			OutFile << "SE142.hca";
			break;
		case 0x8f:
			OutFile << "SE143.hca";
			break;
		case 0x90:
			OutFile << "SE144.hca";
			break;
		case 0x91:
			OutFile << "SE145.hca";
			break;
		case 0x92:
			OutFile << "SE150.hca";
			break;
		case 0x93:
			OutFile << "SE155.hca";
			break;
		case 0x94:
			OutFile << "SE156.hca";
			break;
		case 0x95:
			OutFile << "SE200.hca";
			break;
		case 0x96:
			OutFile << "SE202.hca";
			break;
		case 0x97:
			OutFile << "SE206.hca";
			break;
		case 0x98:
			OutFile << "SE207.hca";
			break;
		case 0x99:
			OutFile << "SE210.hca";
			break;
		case 0x9a:
			OutFile << "SE211.hca";
			break;
		case 0x9b:
			OutFile << "SE404.hca";
			break;
		case 0x9c:
			OutFile << "SE405.hca";
			break;
		case 0x9d:
			OutFile << "SE500.hca";
			break;
		case 0x9e:
			OutFile << "SE600.hca";
			break;
		case 0x9f:
			OutFile << "SKI_003.hca";
			break;
		case 0xa0:
			OutFile << "SKU_003.hca";
			break;
		case 0xa1:
			OutFile << "SKY_003.hca";
			break;
		case 0xa2:
			OutFile << "SMA_003.hca";
			break;
		case 0xa3:
			OutFile << "SSA_003.hca";
			break;

		default:
			OutFile << "Unknown SE " << hex << (unsigned int)SE;
			break;
		}
}

void WriteTsukkomi(char Tsukkomi, ofstream& OutFile)
{
	switch (Tsukkomi)
	{
		case 0x00:
			OutFile << "TKA0020A.gim";
			break;
		case 0x01:
			OutFile << "TKA0020B.gim";
			break;
		case 0x02:
			OutFile << "TKA0020C.gim";
			break;
		case 0x03:
			OutFile << "TKA0032A.gim";
			break;
		case 0x04:
			OutFile << "TKA0032B.gim";
			break;
		case 0x05:
			OutFile << "TKA0032C.gim";
			break;
		case 0x06:
			OutFile << "TKA0032D.gim";
			break;
		case 0x07:
			OutFile << "TKA0036A.gim";
			break;
		case 0x08:
			OutFile << "TKA0036B.gim";
			break;
		case 0x09:
			OutFile << "TKA0036C.gim";
			break;
		case 0x0a:
			OutFile << "TKA0036D.gim";
			break;
		case 0x0b:
			OutFile << "TKA0038A.gim";
			break;
		case 0x0c:
			OutFile << "TKA0038B.gim";
			break;
		case 0x0d:
			OutFile << "TKA0038C.gim";
			break;
		case 0x0e:
			OutFile << "TKA0043A.gim";
			break;
		case 0x0f:
			OutFile << "TKA0043B.gim";
			break;
		case 0x10:
			OutFile << "TKA0043C.gim";
			break;
		case 0x11:
			OutFile << "TKA0056A.gim";
			break;
		case 0x12:
			OutFile << "TKA0056B.gim";
			break;
		case 0x13:
			OutFile << "TKA0056C.gim";
			break;
		case 0x14:
			OutFile << "TKA0056D.gim";
			break;
		case 0x15:
			OutFile << "TKA0070A.gim";
			break;
		case 0x16:
			OutFile << "TKA0070B.gim";
			break;
		case 0x17:
			OutFile << "TKA0070C.gim";
			break;
		case 0x18:
			OutFile << "TKA0070D.gim";
			break;
		case 0x19:
			OutFile << "TKA0070E.gim";
			break;
		case 0x1a:
			OutFile << "TKA0078A.gim";
			break;
		case 0x1b:
			OutFile << "TKA0078B.gim";
			break;
		case 0x1c:
			OutFile << "TKA0078C.gim";
			break;
		case 0x1d:
			OutFile << "TKA0078D.gim";
			break;
		case 0x1e:
			OutFile << "TKA0082A.gim";
			break;
		case 0x1f:
			OutFile << "TKA0082B.gim";
			break;
		case 0x20:
			OutFile << "TKA0082C.gim";
			break;
		case 0x21:
			OutFile << "TKA0082D.gim";
			break;
		case 0x22:
			OutFile << "TKA0136A.gim";
			break;
		case 0x23:
			OutFile << "TKA0136B.gim";
			break;
		case 0x24:
			OutFile << "TKA0136C.gim";
			break;
		case 0x25:
			OutFile << "TKA0136D.gim";
			break;
		case 0x26:
			OutFile << "TKA0136E.gim";
			break;
		case 0x27:
			OutFile << "TKA0138A.gim";
			break;
		case 0x28:
			OutFile << "TKA0138B.gim";
			break;
		case 0x29:
			OutFile << "TKA0138C.gim";
			break;
		case 0x2a:
			OutFile << "TKA0138D.gim";
			break;
		case 0x2b:
			OutFile << "TKA0138E.gim";
			break;
		case 0x2c:
			OutFile << "TKA0143A.gim";
			break;
		case 0x2d:
			OutFile << "TKA0143B.gim";
			break;
		case 0x2e:
			OutFile << "TKA0143C.gim";
			break;
		case 0x2f:
			OutFile << "TKA0143D.gim";
			break;
		case 0x30:
			OutFile << "TKA0143E.gim";
			break;
		case 0x31:
			OutFile << "TKB0004A.gim";
			break;
		case 0x32:
			OutFile << "TKB0004B.gim";
			break;
		case 0x33:
			OutFile << "TKB0004C.gim";
			break;
		case 0x34:
			OutFile << "TKB0004D.gim";
			break;
		case 0x35:
			OutFile << "TKB0013A.gim";
			break;
		case 0x36:
			OutFile << "TKB0013B.gim";
			break;
		case 0x37:
			OutFile << "TKB0013C.gim";
			break;
		case 0x38:
			OutFile << "TKB0013D.gim";
			break;
		case 0x39:
			OutFile << "TKB0020A.gim";
			break;
		case 0x3a:
			OutFile << "TKB0020B.gim";
			break;
		case 0x3b:
			OutFile << "TKB0020C.gim";
			break;
		case 0x3c:
			OutFile << "TKB0020D.gim";
			break;
		case 0x3d:
			OutFile << "TKB0025A.gim";
			break;
		case 0x3e:
			OutFile << "TKB0025B.gim";
			break;
		case 0x3f:
			OutFile << "TKB0025C.gim";
			break;
		case 0x40:
			OutFile << "TKB0025D.gim";
			break;
		case 0x41:
			OutFile << "TKB0033A.gim";
			break;
		case 0x42:
			OutFile << "TKB0033B.gim";
			break;
		case 0x43:
			OutFile << "TKB0033C.gim";
			break;
		case 0x44:
			OutFile << "TKB0033D.gim";
			break;
		case 0x45:
			OutFile << "TKB0048A.gim";
			break;
		case 0x46:
			OutFile << "TKB0048B.gim";
			break;
		case 0x47:
			OutFile << "TKB0063A.gim";
			break;
		case 0x48:
			OutFile << "TKB0063B.gim";
			break;
		case 0x49:
			OutFile << "TKB0063C.gim";
			break;
		case 0x4a:
			OutFile << "TKB0063D.gim";
			break;
		case 0x4b:
			OutFile << "TKB0083A.gim";
			break;
		case 0x4c:
			OutFile << "TKB0083B.gim";
			break;
		case 0x4d:
			OutFile << "TKB0083C.gim";
			break;
		case 0x4e:
			OutFile << "TKB0083D.gim";
			break;
		case 0x4f:
			OutFile << "TKB0083E.gim";
			break;
		case 0x50:
			OutFile << "TKB0103A.gim";
			break;
		case 0x51:
			OutFile << "TKB0103B.gim";
			break;
		case 0x52:
			OutFile << "TKB0103C.gim";
			break;
		case 0x53:
			OutFile << "TKB0103D.gim";
			break;
		case 0x54:
			OutFile << "TKB0123A.gim";
			break;
		case 0x55:
			OutFile << "TKB0123B.gim";
			break;
		case 0x56:
			OutFile << "TKB0123C.gim";
			break;
		case 0x57:
			OutFile << "TKB0123D.gim";
			break;
		case 0x58:
			OutFile << "TKC0035A.gim";
			break;
		case 0x59:
			OutFile << "TKC0035B.gim";
			break;
		case 0x5a:
			OutFile << "TKC0035C.gim";
			break;
		case 0x5b:
			OutFile << "TKC0043A.gim";
			break;
		case 0x5c:
			OutFile << "TKC0043B.gim";
			break;
		case 0x5d:
			OutFile << "TKC0055A.gim";
			break;
		case 0x5e:
			OutFile << "TKC0055B.gim";
			break;
		case 0x5f:
			OutFile << "TKC0055C.gim";
			break;
		case 0x60:
			OutFile << "TKC0055D.gim";
			break;
		case 0x61:
			OutFile << "TKC0067A.gim";
			break;
		case 0x62:
			OutFile << "TKC0067B.gim";
			break;
		case 0x63:
			OutFile << "TKC0067C.gim";
			break;
		case 0x64:
			OutFile << "TKC0093A.gim";
			break;
		case 0x65:
			OutFile << "TKC0093B.gim";
			break;
		case 0x66:
			OutFile << "TKC0093C.gim";
			break;
		case 0x67:
			OutFile << "TKC0093D.gim";
			break;
		case 0x68:
			OutFile << "TKC0100A.gim";
			break;
		case 0x69:
			OutFile << "TKC0100B.gim";
			break;
		case 0x6a:
			OutFile << "TKC0100C.gim";
			break;
		case 0x6b:
			OutFile << "TKC0100D.gim";
			break;
		case 0x6c:
			OutFile << "TKC0105A.gim";
			break;
		case 0x6d:
			OutFile << "TKC0105B.gim";
			break;
		case 0x6e:
			OutFile << "TKC0105C.gim";
			break;
		case 0x6f:
			OutFile << "TKC0105D.gim";
			break;
		case 0x70:
			OutFile << "TKC0108A.gim";
			break;
		case 0x71:
			OutFile << "TKC0108B.gim";
			break;
		case 0x72:
			OutFile << "TKC0108C.gim";
			break;
		case 0x73:
			OutFile << "TKC0108D.gim";
			break;
		case 0x74:
			OutFile << "TKC0125A.gim";
			break;
		case 0x75:
			OutFile << "TKC0125B.gim";
			break;
		case 0x76:
			OutFile << "TKC0125C.gim";
			break;
		case 0x77:
			OutFile << "TKC0125D.gim";
			break;
		case 0x78:
			OutFile << "TKC0175A.gim";
			break;
		case 0x79:
			OutFile << "TKC0175B.gim";
			break;
		case 0x7a:
			OutFile << "TKC0175C.gim";
			break;
		case 0x7b:
			OutFile << "TKC0175D.gim";
			break;
		case 0x7c:
			OutFile << "TKC0175E.gim";
			break;
		case 0x7d:
			OutFile << "TKC0175F.gim";
			break;
		case 0x7e:
			OutFile << "TKD0003A.gim";
			break;
		case 0x7f:
			OutFile << "TKD0003B.gim";
			break;
		case 0x80:
			OutFile << "TKD0003C.gim";
			break;
		case 0x81:
			OutFile << "TKD0003D.gim";
			break;
		case 0x82:
			OutFile << "TKD0003E.gim";
			break;
		case 0x83:
			OutFile << "TKD0007A.gim";
			break;
		case 0x84:
			OutFile << "TKD0007B.gim";
			break;
		case 0x85:
			OutFile << "TKD0007C.gim";
			break;
		case 0x86:
			OutFile << "TKD0007D.gim";
			break;
		case 0x87:
			OutFile << "TKD0026A.gim";
			break;
		case 0x88:
			OutFile << "TKD0026B.gim";
			break;
		case 0x89:
			OutFile << "TKD0026C.gim";
			break;
		case 0x8a:
			OutFile << "TKD0026D.gim";
			break;
		case 0x8b:
			OutFile << "TKD0026E.gim";
			break;
		case 0x8c:
			OutFile << "TKD0026F.gim";
			break;
		case 0x8d:
			OutFile << "TKD0038A.gim";
			break;
		case 0x8e:
			OutFile << "TKD0038B.gim";
			break;
		case 0x8f:
			OutFile << "TKD0038C.gim";
			break;
		case 0x90:
			OutFile << "TKD0038D.gim";
			break;
		case 0x91:
			OutFile << "TKD0053A.gim";
			break;
		case 0x92:
			OutFile << "TKD0053B.gim";
			break;
		case 0x93:
			OutFile << "TKD0053C.gim";
			break;
		case 0x94:
			OutFile << "TKD0053D.gim";
			break;
		case 0x95:
			OutFile << "TKD0053E.gim";
			break;
		case 0x96:
			OutFile << "TKD0058A.gim";
			break;
		case 0x97:
			OutFile << "TKD0058B.gim";
			break;
		case 0x98:
			OutFile << "TKD0058C.gim";
			break;
		case 0x99:
			OutFile << "TKD0058D.gim";
			break;
		case 0x9a:
			OutFile << "TKD0063A.gim";
			break;
		case 0x9b:
			OutFile << "TKD0063B.gim";
			break;
		case 0x9c:
			OutFile << "TKD0072A.gim";
			break;
		case 0x9d:
			OutFile << "TKD0072B.gim";
			break;
		case 0x9e:
			OutFile << "TKD0072C.gim";
			break;
		case 0x9f:
			OutFile << "TKD0072D.gim";
			break;
		case 0xa0:
			OutFile << "TKD0072E.gim";
			break;
		case 0xa1:
			OutFile << "TKD0072F.gim";
			break;
		case 0xa2:
			OutFile << "TKD0072G.gim";
			break;
		case 0xa3:
			OutFile << "TKD0075A.gim";
			break;
		case 0xa4:
			OutFile << "TKD0075B.gim";
			break;
		case 0xa5:
			OutFile << "TKD0075C.gim";
			break;
		case 0xa6:
			OutFile << "TKD0080A.gim";
			break;
		case 0xa7:
			OutFile << "TKD0080B.gim";
			break;
		case 0xa8:
			OutFile << "TKD0080C.gim";
			break;
		case 0xa9:
			OutFile << "TKD0080D.gim";
			break;
		case 0xaa:
			OutFile << "TKD0080E.gim";
			break;
		case 0xab:
			OutFile << "TKD0080F.gim";
			break;
		case 0xac:
			OutFile << "TKD0080G.gim";
			break;
		case 0xad:
			OutFile << "TKD0080H.gim";
			break;
		case 0xae:
			OutFile << "TKE0007A.gim";
			break;
		case 0xaf:
			OutFile << "TKE0007B.gim";
			break;
		case 0xb0:
			OutFile << "TKE0007C.gim";
			break;
		case 0xb1:
			OutFile << "TKE0007D.gim";
			break;
		case 0xb2:
			OutFile << "TKE0013A.gim";
			break;
		case 0xb3:
			OutFile << "TKE0013B.gim";
			break;
		case 0xb4:
			OutFile << "TKE0013C.gim";
			break;
		case 0xb5:
			OutFile << "TKE0013D.gim";
			break;
		case 0xb6:
			OutFile << "TKE0013E.gim";
			break;
		case 0xb7:
			OutFile << "TKE0033A.gim";
			break;
		case 0xb8:
			OutFile << "TKE0033B.gim";
			break;
		case 0xb9:
			OutFile << "TKE0033C.gim";
			break;
		case 0xba:
			OutFile << "TKE0033D.gim";
			break;
		case 0xbb:
			OutFile << "TKE0040A.gim";
			break;
		case 0xbc:
			OutFile << "TKE0040B.gim";
			break;
		case 0xbd:
			OutFile << "TKE0040C.gim";
			break;
		case 0xbe:
			OutFile << "TKE0040D.gim";
			break;
		case 0xbf:
			OutFile << "TKE0044A.gim";
			break;
		case 0xc0:
			OutFile << "TKE0044B.gim";
			break;
		case 0xc1:
			OutFile << "TKE0044C.gim";
			break;
		case 0xc2:
			OutFile << "TKE0044D.gim";
			break;
		case 0xc3:
			OutFile << "TKE0056A.gim";
			break;
		case 0xc4:
			OutFile << "TKE0056B.gim";
			break;
		case 0xc5:
			OutFile << "TKE0056C.gim";
			break;
		case 0xc6:
			OutFile << "TKE0056D.gim";
			break;
		case 0xc7:
			OutFile << "TKE0064A.gim";
			break;
		case 0xc8:
			OutFile << "TKE0064B.gim";
			break;
		case 0xc9:
			OutFile << "TKE0064C.gim";
			break;
		case 0xca:
			OutFile << "TKE0064D.gim";
			break;
		case 0xcb:
			OutFile << "TKE0064E.gim";
			break;
		case 0xcc:
			OutFile << "TKF0011A.gim";
			break;
		case 0xcd:
			OutFile << "TKF0011B.gim";
			break;
		case 0xce:
			OutFile << "TKF0011C.gim";
			break;
		case 0xcf:
			OutFile << "TKF0011D.gim";
			break;
		case 0xd0:
			OutFile << "TKF0015A.gim";
			break;
		case 0xd1:
			OutFile << "TKF0015B.gim";
			break;
		case 0xd2:
			OutFile << "TKF0032A.gim";
			break;
		case 0xd3:
			OutFile << "TKF0032B.gim";
			break;
		case 0xd4:
			OutFile << "TKF0032C.gim";
			break;
		case 0xd5:
			OutFile << "TKF0034A.gim";
			break;
		case 0xd6:
			OutFile << "TKF0034B.gim";
			break;
		case 0xd7:
			OutFile << "TKF0034C.gim";
			break;
		case 0xd8:
			OutFile << "TKF0043D.gim";
			break;
		case 0xd9:
			OutFile << "TKF0043E.gim";
			break;
		case 0xda:
			OutFile << "TKG0005A.gim";
			break;
		case 0xdb:
			OutFile << "TKG0005B.gim";
			break;
		case 0xdc:
			OutFile << "TKG0005C.gim";
			break;
		case 0xdd:
			OutFile << "TKG0005D.gim";
			break;
		case 0xde:
			OutFile << "TKG0011A.gim";
			break;
		case 0xdf:
			OutFile << "TKG0011B.gim";
			break;
		case 0xe0:
			OutFile << "TKG0011C.gim";
			break;
		case 0xe1:
			OutFile << "TKG0011D.gim";
			break;
		case 0xe2:
			OutFile << "TKG0014A.gim";
			break;
		case 0xe3:
			OutFile << "TKG0014B.gim";
			break;
		case 0xe4:
			OutFile << "TKG0014C.gim";
			break;
		case 0xe5:
			OutFile << "TKG0014D.gim";
			break;
		case 0xe6:
			OutFile << "TKG0017A.gim";
			break;
		case 0xe7:
			OutFile << "TKG0017B.gim";
			break;
		case 0xe8:
			OutFile << "TKG0017C.gim";
			break;
		case 0xe9:
			OutFile << "TKG0017D.gim";
			break;
		case 0xea:
			OutFile << "TKG0033A.gim";
			break;
		case 0xeb:
			OutFile << "TKG0033B.gim";
			break;
		case 0xec:
			OutFile << "TKG0033C.gim";
			break;
		case 0xed:
			OutFile << "TKG0033D.gim";
			break;
		case 0xee:
			OutFile << "TKG0038A.gim";
			break;
		case 0xef:
			OutFile << "TKG0038B.gim";
			break;
		case 0xf0:
			OutFile << "TKG0038C.gim";
			break;
		case 0xf1:
			OutFile << "TKG0038D.gim";
			break;

		default:
			OutFile << "Unknown tsukkomi";
			break;

	}
}