#if (!OPTIMIZED && !OPTIMIZED2)
using System;
using System.Text;
using System.IO;
using System.IO.Compression;
using Base.Message;

namespace Server.Restapi
{
public enum BaseActions
{
None,
Role,
Options,
Version,
Accounts,
}
public enum Methods
{
None,
Put,
Delete,
}
public enum AccountActions
{
None,
Userz,
}
public partial class RestapiUriParser
{
public bool Final;
public bool IsFinal { get { return Final; }}
public bool Error;
public bool IsError { get { return Error; }}
private int state;
private int boolExPosition;
public ByteArrayPart DomainName;
public ByteArrayPart Signature;
public ByteArrayPart Authname;
public ByteArrayPart UsersId;
public ByteArrayPart Username;
public long Timestamp;
public int Count;
public int StartIndex;
public BaseActions BaseAction;
public Methods Method;
public AccountActions AccountAction;
partial void OnSetDefaultValue();
public void SetDefaultValue()
{
Final = false;
Error = false;
state = State0;
boolExPosition = int.MinValue;
BaseAction = BaseActions.None;
Method = Methods.None;
AccountAction = AccountActions.None;
DomainName.SetDefaultValue();
Signature.SetDefaultValue();
Authname.SetDefaultValue();
UsersId.SetDefaultValue();
Username.SetDefaultValue();
Timestamp = long.MinValue;
Count = int.MinValue;
StartIndex = int.MinValue;
OnSetDefaultValue();
}
public void SetArray(byte[] bytes)
{
DomainName.Bytes = bytes;
Signature.Bytes = bytes;
Authname.Bytes = bytes;
UsersId.Bytes = bytes;
Username.Bytes = bytes;
}
#region enum States
const int State0 = 0;
const int State1 = 1;
const int State2 = 2;
const int State3 = 3;
const int State4 = 4;
const int State5 = 5;
const int State6 = 6;
const int State7 = 7;
const int State8 = 8;
const int State9 = 9;
const int State10 = 10;
const int State11 = 11;
const int State12 = 12;
const int State13 = 13;
const int State14 = 14;
const int State15 = 15;
const int State16 = 16;
const int State17 = 17;
const int State18 = 18;
const int State19 = 19;
const int State20 = 20;
const int State21 = 21;
const int State22 = 22;
const int State23 = 23;
const int State24 = 24;
const int State25 = 25;
const int State26 = 26;
const int State27 = 27;
const int State28 = 28;
const int State29 = 29;
const int State30 = 30;
const int State31 = 31;
const int State32 = 32;
const int State33 = 33;
const int State34 = 34;
const int State35 = 35;
const int State36 = 36;
const int State37 = 37;
const int State38 = 38;
const int State39 = 39;
const int State40 = 40;
const int State41 = 41;
const int State42 = 42;
const int State43 = 43;
const int State44 = 44;
const int State45 = 45;
const int State46 = 46;
const int State47 = 47;
const int State48 = 48;
const int State49 = 49;
const int State50 = 50;
const int State51 = 51;
const int State52 = 52;
const int State53 = 53;
const int State54 = 54;
const int State55 = 55;
const int State56 = 56;
const int State57 = 57;
const int State58 = 58;
const int State59 = 59;
const int State60 = 60;
const int State61 = 61;
const int State62 = 62;
const int State63 = 63;
const int State64 = 64;
const int State65 = 65;
const int State66 = 66;
const int State67 = 67;
const int State68 = 68;
const int State69 = 69;
const int State70 = 70;
const int State71 = 71;
const int State72 = 72;
const int State73 = 73;
const int State74 = 74;
const int State75 = 75;
const int State76 = 76;
const int State77 = 77;
const int State78 = 78;
const int State79 = 79;
const int State80 = 80;
const int State81 = 81;
const int State82 = 82;
const int State83 = 83;
const int State84 = 84;
const int State85 = 85;
const int State86 = 86;
const int State87 = 87;
const int State88 = 88;
const int State89 = 89;
const int State90 = 90;
const int State91 = 91;
const int State92 = 92;
const int State93 = 93;
const int State94 = 94;
const int State95 = 95;
const int State96 = 96;
const int State97 = 97;
const int State98 = 98;
const int State99 = 99;
const int State100 = 100;
const int State101 = 101;
const int State102 = 102;
const int State103 = 103;
const int State104 = 104;
const int State105 = 105;
const int State106 = 106;
const int State107 = 107;
const int State108 = 108;
const int State109 = 109;
const int State110 = 110;
const int State111 = 111;
const int State112 = 112;
const int State113 = 113;
const int State114 = 114;
const int State115 = 115;
const int State116 = 116;
const int State117 = 117;
const int State118 = 118;
const int State119 = 119;
const int State120 = 120;
const int State121 = 121;
const int State122 = 122;
const int State123 = 123;
const int State124 = 124;
const int State125 = 125;
const int State126 = 126;
const int State127 = 127;
const int State128 = 128;
const int State129 = 129;
const int State130 = 130;
const int State131 = 131;
const int State132 = 132;
const int State133 = 133;
const int State134 = 134;
const int State135 = 135;
const int State136 = 136;
const int State137 = 137;
const int State138 = 138;
const int State139 = 139;
const int State140 = 140;
const int State141 = 141;
const int State142 = 142;
const int State143 = 143;
const int State144 = 144;
const int State145 = 145;
const int State146 = 146;
const int State147 = 147;
const int State148 = 148;
const int State149 = 149;
const int State150 = 150;
const int State151 = 151;
const int State152 = 152;
const int State153 = 153;
const int State154 = 154;
const int State155 = 155;
const int State156 = 156;
const int State157 = 157;
const int State158 = 158;
const int State159 = 159;
const int State160 = 160;
const int State161 = 161;
const int State162 = 162;
const int State163 = 163;
const int State164 = 164;
const int State165 = 165;
const int State166 = 166;
const int State167 = 167;
const int State168 = 168;
const int State169 = 169;
const int State170 = 170;
const int State171 = 171;
const int State172 = 172;
const int State173 = 173;
const int State174 = 174;
const int State175 = 175;
const int State176 = 176;
const int State177 = 177;
const int State178 = 178;
const int State179 = 179;
const int State180 = 180;
const int State181 = 181;
const int State182 = 182;
const int State183 = 183;
const int State184 = 184;
const int State185 = 185;
const int State186 = 186;
const int State187 = 187;
const int State188 = 188;
const int State189 = 189;
const int State190 = 190;
const int State191 = 191;
const int State192 = 192;
const int State193 = 193;
const int State194 = 194;
const int State195 = 195;
const int State196 = 196;
const int State197 = 197;
const int State198 = 198;
const int State199 = 199;
const int State200 = 200;
const int State201 = 201;
const int State202 = 202;
const int State203 = 203;
const int State204 = 204;
const int State205 = 205;
const int State206 = 206;
const int State207 = 207;
const int State208 = 208;
const int State209 = 209;
const int State210 = 210;
const int State211 = 211;
const int State212 = 212;
const int State213 = 213;
const int State214 = 214;
const int State215 = 215;
const int State216 = 216;
const int State217 = 217;
const int State218 = 218;
const int State219 = 219;
const int State220 = 220;
const int State221 = 221;
const int State222 = 222;
const int State223 = 223;
const int State224 = 224;
const int State225 = 225;
const int State226 = 226;
const int State227 = 227;
const int State228 = 228;
const int State229 = 229;
const int State230 = 230;
const int State231 = 231;
const int State232 = 232;
const int State233 = 233;
const int State234 = 234;
const int State235 = 235;
const int State236 = 236;
const int State237 = 237;
const int State238 = 238;
const int State239 = 239;
const int State240 = 240;
const int State241 = 241;
const int State242 = 242;
const int State243 = 243;
const int State244 = 244;
const int State245 = 245;
const int State246 = 246;
const int State247 = 247;
const int State248 = 248;
const int State249 = 249;
const int State250 = 250;
const int State251 = 251;
const int State252 = 252;
const int State253 = 253;
const int State254 = 254;
const int State255 = 255;
const int State256 = 256;
const int State257 = 257;
const int State258 = 258;
const int State259 = 259;
const int State260 = 260;
const int State261 = 261;
const int State262 = 262;
const int State263 = 263;
const int State264 = 264;
const int State265 = 265;
const int State266 = 266;
const int State267 = 267;
const int State268 = 268;
const int State269 = 269;
const int State270 = 270;
const int State271 = 271;
const int State272 = 272;
const int State273 = 273;
const int State274 = 274;
const int State275 = 275;
const int State276 = 276;
const int State277 = 277;
const int State278 = 278;
const int State279 = 279;
#endregion
#region States Tables
private static int[] table0;
private static int[] table1;
private static int[] table2;
private static int[] table3;
private static int[] table4;
private static int[] table5;
private static int[] table6;
private static int[] table7;
private static int[] table8;
private static int[] table9;
private static int[] table10;
private static int[] table11;
private static int[] table12;
private static int[] table13;
private static int[] table14;
private static int[] table15;
private static int[] table16;
private static int[] table17;
private static int[] table18;
private static int[] table19;
private static int[] table20;
private static int[] table21;
private static int[] table22;
private static int[] table23;
private static int[] table24;
private static int[] table25;
private static int[] table26;
private static int[] table27;
private static int[] table28;
private static int[] table29;
private static int[] table30;
private static int[] table31;
private static int[] table32;
private static int[] table33;
private static int[] table34;
private static int[] table35;
private static int[] table36;
private static int[] table37;
private static int[] table38;
private static int[] table39;
private static int[] table40;
private static int[] table41;
private static int[] table42;
private static int[] table43;
private static int[] table44;
private static int[] table45;
private static int[] table46;
private static int[] table47;
private static int[] table48;
private static int[] table49;
private static int[] table50;
private static int[] table51;
private static int[] table52;
private static int[] table53;
private static int[] table54;
private static int[] table55;
private static int[] table56;
private static int[] table57;
private static int[] table58;
private static int[] table59;
private static int[] table60;
private static int[] table61;
private static int[] table62;
private static int[] table63;
private static int[] table64;
private static int[] table65;
private static int[] table66;
private static int[] table67;
private static int[] table68;
private static int[] table69;
private static int[] table70;
private static int[] table71;
private static int[] table72;
private static int[] table73;
private static int[] table74;
private static int[] table75;
private static int[] table76;
private static int[] table77;
private static int[] table78;
private static int[] table79;
private static int[] table80;
private static int[] table81;
private static int[] table82;
private static int[] table83;
private static int[] table84;
private static int[] table85;
private static int[] table86;
private static int[] table87;
private static int[] table88;
private static int[] table89;
private static int[] table90;
private static int[] table91;
private static int[] table92;
private static int[] table93;
private static int[] table94;
private static int[] table95;
private static int[] table96;
private static int[] table97;
private static int[] table98;
private static int[] table99;
private static int[] table100;
private static int[] table101;
private static int[] table102;
private static int[] table103;
private static int[] table104;
private static int[] table105;
private static int[] table106;
private static int[] table107;
private static int[] table108;
private static int[] table109;
private static int[] table110;
private static int[] table111;
private static int[] table112;
private static int[] table113;
private static int[] table114;
private static int[] table115;
private static int[] table116;
private static int[] table117;
private static int[] table118;
private static int[] table119;
private static int[] table120;
private static int[] table121;
private static int[] table122;
private static int[] table123;
private static int[] table124;
private static int[] table125;
private static int[] table126;
private static int[] table127;
private static int[] table128;
private static int[] table129;
private static int[] table130;
private static int[] table131;
private static int[] table132;
private static int[] table133;
private static int[] table134;
private static int[] table135;
private static int[] table136;
private static int[] table137;
private static int[] table138;
private static int[] table139;
private static int[] table140;
private static int[] table141;
private static int[] table142;
private static int[] table143;
private static int[] table144;
private static int[] table145;
private static int[] table146;
private static int[] table147;
private static int[] table148;
private static int[] table149;
private static int[] table150;
private static int[] table151;
private static int[] table152;
private static int[] table153;
private static int[] table154;
private static int[] table155;
private static int[] table156;
private static int[] table157;
private static int[] table158;
private static int[] table159;
private static int[] table160;
private static int[] table161;
private static int[] table162;
private static int[] table163;
private static int[] table164;
private static int[] table165;
private static int[] table166;
private static int[] table167;
private static int[] table168;
private static int[] table169;
private static int[] table170;
private static int[] table171;
private static int[] table172;
private static int[] table173;
private static int[] table174;
private static int[] table175;
private static int[] table176;
private static int[] table177;
private static int[] table178;
private static int[] table179;
private static int[] table180;
private static int[] table181;
private static int[] table182;
private static int[] table183;
private static int[] table184;
private static int[] table185;
private static int[] table186;
private static int[] table187;
private static int[] table188;
private static int[] table189;
private static int[] table190;
private static int[] table191;
private static int[] table192;
private static int[] table193;
private static int[] table194;
private static int[] table195;
private static int[] table196;
private static int[] table197;
private static int[] table198;
private static int[] table199;
private static int[] table200;
private static int[] table201;
private static int[] table202;
private static int[] table203;
private static int[] table204;
private static int[] table205;
private static int[] table206;
private static int[] table207;
private static int[] table208;
private static int[] table209;
private static int[] table210;
private static int[] table211;
private static int[] table212;
private static int[] table213;
private static int[] table214;
private static int[] table215;
private static int[] table216;
private static int[] table217;
private static int[] table218;
private static int[] table219;
private static int[] table220;
private static int[] table221;
private static int[] table222;
private static int[] table223;
private static int[] table224;
private static int[] table225;
private static int[] table226;
private static int[] table227;
private static int[] table228;
private static int[] table229;
private static int[] table230;
private static int[] table231;
private static int[] table232;
private static int[] table233;
private static int[] table234;
private static int[] table235;
private static int[] table236;
private static int[] table237;
private static int[] table238;
private static int[] table239;
private static int[] table240;
private static int[] table241;
private static int[] table242;
private static int[] table243;
private static int[] table244;
private static int[] table245;
private static int[] table246;
private static int[] table247;
private static int[] table248;
private static int[] table249;
private static int[] table250;
private static int[] table251;
private static int[] table252;
private static int[] table253;
private static int[] table254;
private static int[] table255;
private static int[] table256;
private static int[] table257;
private static int[] table258;
private static int[] table259;
private static int[] table260;
private static int[] table261;
private static int[] table262;
private static int[] table263;
private static int[] table264;
private static int[] table265;
private static int[] table266;
private static int[] table267;
private static int[] table268;
private static int[] table269;
private static int[] table270;
private static int[] table271;
private static int[] table272;
private static int[] table273;
private static int[] table274;
private static int[] table275;
private static int[] table276;
private static int[] table277;
private static int[] table278;
#endregion
#region void LoadTables(..)
public static void LoadTables()
{
LoadTables(null);
}
public static void LoadTables(string path)
{
const int maxItems = byte.MaxValue + 1;
const int maxBytes = sizeof(Int32) * maxItems;
if(path==null) path=Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
using (var reader = new DeflateStream(File.OpenRead(path+"\\Server.Restapi.dfa"), CompressionMode.Decompress))
{
byte[] buffer = new byte[maxBytes];
table0 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table0, 0, maxBytes);
table1 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table1, 0, maxBytes);
table2 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table2, 0, maxBytes);
table3 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table3, 0, maxBytes);
table4 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table4, 0, maxBytes);
table5 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table5, 0, maxBytes);
table6 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table6, 0, maxBytes);
table7 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table7, 0, maxBytes);
table8 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table8, 0, maxBytes);
table9 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table9, 0, maxBytes);
table10 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table10, 0, maxBytes);
table11 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table11, 0, maxBytes);
table12 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table12, 0, maxBytes);
table13 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table13, 0, maxBytes);
table14 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table14, 0, maxBytes);
table15 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table15, 0, maxBytes);
table16 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table16, 0, maxBytes);
table17 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table17, 0, maxBytes);
table18 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table18, 0, maxBytes);
table19 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table19, 0, maxBytes);
table20 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table20, 0, maxBytes);
table21 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table21, 0, maxBytes);
table22 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table22, 0, maxBytes);
table23 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table23, 0, maxBytes);
table24 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table24, 0, maxBytes);
table25 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table25, 0, maxBytes);
table26 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table26, 0, maxBytes);
table27 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table27, 0, maxBytes);
table28 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table28, 0, maxBytes);
table29 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table29, 0, maxBytes);
table30 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table30, 0, maxBytes);
table31 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table31, 0, maxBytes);
table32 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table32, 0, maxBytes);
table33 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table33, 0, maxBytes);
table34 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table34, 0, maxBytes);
table35 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table35, 0, maxBytes);
table36 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table36, 0, maxBytes);
table37 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table37, 0, maxBytes);
table38 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table38, 0, maxBytes);
table39 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table39, 0, maxBytes);
table40 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table40, 0, maxBytes);
table41 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table41, 0, maxBytes);
table42 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table42, 0, maxBytes);
table43 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table43, 0, maxBytes);
table44 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table44, 0, maxBytes);
table45 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table45, 0, maxBytes);
table46 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table46, 0, maxBytes);
table47 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table47, 0, maxBytes);
table48 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table48, 0, maxBytes);
table49 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table49, 0, maxBytes);
table50 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table50, 0, maxBytes);
table51 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table51, 0, maxBytes);
table52 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table52, 0, maxBytes);
table53 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table53, 0, maxBytes);
table54 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table54, 0, maxBytes);
table55 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table55, 0, maxBytes);
table56 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table56, 0, maxBytes);
table57 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table57, 0, maxBytes);
table58 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table58, 0, maxBytes);
table59 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table59, 0, maxBytes);
table60 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table60, 0, maxBytes);
table61 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table61, 0, maxBytes);
table62 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table62, 0, maxBytes);
table63 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table63, 0, maxBytes);
table64 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table64, 0, maxBytes);
table65 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table65, 0, maxBytes);
table66 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table66, 0, maxBytes);
table67 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table67, 0, maxBytes);
table68 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table68, 0, maxBytes);
table69 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table69, 0, maxBytes);
table70 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table70, 0, maxBytes);
table71 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table71, 0, maxBytes);
table72 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table72, 0, maxBytes);
table73 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table73, 0, maxBytes);
table74 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table74, 0, maxBytes);
table75 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table75, 0, maxBytes);
table76 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table76, 0, maxBytes);
table77 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table77, 0, maxBytes);
table78 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table78, 0, maxBytes);
table79 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table79, 0, maxBytes);
table80 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table80, 0, maxBytes);
table81 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table81, 0, maxBytes);
table82 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table82, 0, maxBytes);
table83 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table83, 0, maxBytes);
table84 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table84, 0, maxBytes);
table85 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table85, 0, maxBytes);
table86 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table86, 0, maxBytes);
table87 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table87, 0, maxBytes);
table88 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table88, 0, maxBytes);
table89 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table89, 0, maxBytes);
table90 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table90, 0, maxBytes);
table91 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table91, 0, maxBytes);
table92 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table92, 0, maxBytes);
table93 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table93, 0, maxBytes);
table94 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table94, 0, maxBytes);
table95 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table95, 0, maxBytes);
table96 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table96, 0, maxBytes);
table97 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table97, 0, maxBytes);
table98 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table98, 0, maxBytes);
table99 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table99, 0, maxBytes);
table100 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table100, 0, maxBytes);
table101 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table101, 0, maxBytes);
table102 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table102, 0, maxBytes);
table103 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table103, 0, maxBytes);
table104 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table104, 0, maxBytes);
table105 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table105, 0, maxBytes);
table106 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table106, 0, maxBytes);
table107 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table107, 0, maxBytes);
table108 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table108, 0, maxBytes);
table109 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table109, 0, maxBytes);
table110 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table110, 0, maxBytes);
table111 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table111, 0, maxBytes);
table112 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table112, 0, maxBytes);
table113 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table113, 0, maxBytes);
table114 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table114, 0, maxBytes);
table115 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table115, 0, maxBytes);
table116 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table116, 0, maxBytes);
table117 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table117, 0, maxBytes);
table118 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table118, 0, maxBytes);
table119 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table119, 0, maxBytes);
table120 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table120, 0, maxBytes);
table121 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table121, 0, maxBytes);
table122 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table122, 0, maxBytes);
table123 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table123, 0, maxBytes);
table124 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table124, 0, maxBytes);
table125 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table125, 0, maxBytes);
table126 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table126, 0, maxBytes);
table127 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table127, 0, maxBytes);
table128 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table128, 0, maxBytes);
table129 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table129, 0, maxBytes);
table130 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table130, 0, maxBytes);
table131 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table131, 0, maxBytes);
table132 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table132, 0, maxBytes);
table133 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table133, 0, maxBytes);
table134 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table134, 0, maxBytes);
table135 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table135, 0, maxBytes);
table136 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table136, 0, maxBytes);
table137 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table137, 0, maxBytes);
table138 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table138, 0, maxBytes);
table139 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table139, 0, maxBytes);
table140 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table140, 0, maxBytes);
table141 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table141, 0, maxBytes);
table142 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table142, 0, maxBytes);
table143 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table143, 0, maxBytes);
table144 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table144, 0, maxBytes);
table145 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table145, 0, maxBytes);
table146 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table146, 0, maxBytes);
table147 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table147, 0, maxBytes);
table148 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table148, 0, maxBytes);
table149 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table149, 0, maxBytes);
table150 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table150, 0, maxBytes);
table151 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table151, 0, maxBytes);
table152 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table152, 0, maxBytes);
table153 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table153, 0, maxBytes);
table154 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table154, 0, maxBytes);
table155 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table155, 0, maxBytes);
table156 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table156, 0, maxBytes);
table157 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table157, 0, maxBytes);
table158 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table158, 0, maxBytes);
table159 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table159, 0, maxBytes);
table160 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table160, 0, maxBytes);
table161 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table161, 0, maxBytes);
table162 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table162, 0, maxBytes);
table163 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table163, 0, maxBytes);
table164 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table164, 0, maxBytes);
table165 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table165, 0, maxBytes);
table166 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table166, 0, maxBytes);
table167 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table167, 0, maxBytes);
table168 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table168, 0, maxBytes);
table169 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table169, 0, maxBytes);
table170 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table170, 0, maxBytes);
table171 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table171, 0, maxBytes);
table172 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table172, 0, maxBytes);
table173 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table173, 0, maxBytes);
table174 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table174, 0, maxBytes);
table175 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table175, 0, maxBytes);
table176 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table176, 0, maxBytes);
table177 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table177, 0, maxBytes);
table178 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table178, 0, maxBytes);
table179 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table179, 0, maxBytes);
table180 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table180, 0, maxBytes);
table181 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table181, 0, maxBytes);
table182 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table182, 0, maxBytes);
table183 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table183, 0, maxBytes);
table184 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table184, 0, maxBytes);
table185 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table185, 0, maxBytes);
table186 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table186, 0, maxBytes);
table187 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table187, 0, maxBytes);
table188 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table188, 0, maxBytes);
table189 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table189, 0, maxBytes);
table190 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table190, 0, maxBytes);
table191 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table191, 0, maxBytes);
table192 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table192, 0, maxBytes);
table193 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table193, 0, maxBytes);
table194 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table194, 0, maxBytes);
table195 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table195, 0, maxBytes);
table196 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table196, 0, maxBytes);
table197 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table197, 0, maxBytes);
table198 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table198, 0, maxBytes);
table199 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table199, 0, maxBytes);
table200 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table200, 0, maxBytes);
table201 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table201, 0, maxBytes);
table202 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table202, 0, maxBytes);
table203 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table203, 0, maxBytes);
table204 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table204, 0, maxBytes);
table205 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table205, 0, maxBytes);
table206 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table206, 0, maxBytes);
table207 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table207, 0, maxBytes);
table208 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table208, 0, maxBytes);
table209 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table209, 0, maxBytes);
table210 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table210, 0, maxBytes);
table211 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table211, 0, maxBytes);
table212 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table212, 0, maxBytes);
table213 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table213, 0, maxBytes);
table214 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table214, 0, maxBytes);
table215 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table215, 0, maxBytes);
table216 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table216, 0, maxBytes);
table217 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table217, 0, maxBytes);
table218 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table218, 0, maxBytes);
table219 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table219, 0, maxBytes);
table220 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table220, 0, maxBytes);
table221 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table221, 0, maxBytes);
table222 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table222, 0, maxBytes);
table223 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table223, 0, maxBytes);
table224 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table224, 0, maxBytes);
table225 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table225, 0, maxBytes);
table226 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table226, 0, maxBytes);
table227 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table227, 0, maxBytes);
table228 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table228, 0, maxBytes);
table229 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table229, 0, maxBytes);
table230 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table230, 0, maxBytes);
table231 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table231, 0, maxBytes);
table232 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table232, 0, maxBytes);
table233 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table233, 0, maxBytes);
table234 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table234, 0, maxBytes);
table235 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table235, 0, maxBytes);
table236 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table236, 0, maxBytes);
table237 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table237, 0, maxBytes);
table238 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table238, 0, maxBytes);
table239 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table239, 0, maxBytes);
table240 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table240, 0, maxBytes);
table241 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table241, 0, maxBytes);
table242 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table242, 0, maxBytes);
table243 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table243, 0, maxBytes);
table244 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table244, 0, maxBytes);
table245 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table245, 0, maxBytes);
table246 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table246, 0, maxBytes);
table247 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table247, 0, maxBytes);
table248 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table248, 0, maxBytes);
table249 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table249, 0, maxBytes);
table250 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table250, 0, maxBytes);
table251 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table251, 0, maxBytes);
table252 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table252, 0, maxBytes);
table253 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table253, 0, maxBytes);
table254 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table254, 0, maxBytes);
table255 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table255, 0, maxBytes);
table256 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table256, 0, maxBytes);
table257 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table257, 0, maxBytes);
table258 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table258, 0, maxBytes);
table259 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table259, 0, maxBytes);
table260 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table260, 0, maxBytes);
table261 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table261, 0, maxBytes);
table262 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table262, 0, maxBytes);
table263 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table263, 0, maxBytes);
table264 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table264, 0, maxBytes);
table265 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table265, 0, maxBytes);
table266 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table266, 0, maxBytes);
table267 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table267, 0, maxBytes);
table268 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table268, 0, maxBytes);
table269 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table269, 0, maxBytes);
table270 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table270, 0, maxBytes);
table271 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table271, 0, maxBytes);
table272 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table272, 0, maxBytes);
table273 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table273, 0, maxBytes);
table274 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table274, 0, maxBytes);
table275 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table275, 0, maxBytes);
table276 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table276, 0, maxBytes);
table277 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table277, 0, maxBytes);
table278 = new int[maxItems];
reader.Read(buffer, 0, buffer.Length);
Buffer.BlockCopy(buffer, 0, table278, 0, maxBytes);
}
}
public static void InitializeAsync(Action<int> callback)
{
RestapiUriParser.InitializeAsync(null, callback);
}
public static void InitializeAsync(string path, Action<int> callback)
{
System.Threading.ThreadPool.QueueUserWorkItem((stateInfo) =>
{
RestapiUriParser.LoadTables();
int time = RestapiUriParser.CompileParseMethod();
if (callback != null)
callback(time);
});
}
public static int CompileParseMethod()
{
int start = Environment.TickCount;
var reader = new RestapiUriParser();
reader.SetDefaultValue();
reader.Parse(new byte[] { 0 }, 0, 1);
return Environment.TickCount - start;
}
#endregion
partial void OnBeforeParse();
partial void OnAfterParse();
#region int Parse(..)
public bool ParseAll(ArraySegment<byte> data)
{
int parsed;
return ParseAll(data.Array, data.Offset, data.Count, out parsed);
}
public bool ParseAll(ArraySegment<byte> data, out int parsed)
{
return ParseAll(data.Array, data.Offset, data.Count, out parsed);
}
public bool ParseAll(byte[] bytes, int offset, int length, out int parsed)
{
parsed = 0;
do
{
Final = false;
parsed += Parse(bytes, offset + parsed, length - parsed);
} while (parsed < length && IsFinal);
return IsFinal;
}
public int Parse(ArraySegment<byte> data)
{
return Parse(data.Array, data.Offset, data.Count);
}
public int Parse(byte[] bytes, int offset, int length)
{
OnBeforeParse();
int i = offset;
switch(state)
{
case State0:
state = table0[bytes[i]];
break;
case State1:
state = table1[bytes[i]];
break;
case State2:
state = table2[bytes[i]];
break;
case State3:
state = table3[bytes[i]];
break;
case State4:
state = table4[bytes[i]];
break;
case State5:
state = table5[bytes[i]];
break;
case State6:
state = table6[bytes[i]];
break;
case State7:
state = table7[bytes[i]];
break;
case State8:
state = table8[bytes[i]];
break;
case State9:
state = table9[bytes[i]];
break;
case State10:
state = table10[bytes[i]];
break;
case State11:
state = table11[bytes[i]];
break;
case State12:
state = table12[bytes[i]];
break;
case State13:
state = table13[bytes[i]];
break;
case State14:
state = table14[bytes[i]];
break;
case State15:
state = table15[bytes[i]];
break;
case State16:
state = table16[bytes[i]];
break;
case State17:
state = table17[bytes[i]];
break;
case State18:
state = table18[bytes[i]];
break;
case State19:
state = table19[bytes[i]];
break;
case State20:
state = table20[bytes[i]];
break;
case State21:
state = table21[bytes[i]];
break;
case State22:
state = table22[bytes[i]];
break;
case State23:
state = table23[bytes[i]];
break;
case State24:
state = table24[bytes[i]];
break;
case State25:
state = table25[bytes[i]];
break;
case State26:
state = table26[bytes[i]];
break;
case State27:
state = table27[bytes[i]];
break;
case State28:
state = table28[bytes[i]];
break;
case State29:
state = table29[bytes[i]];
break;
case State30:
state = table30[bytes[i]];
break;
case State31:
state = table31[bytes[i]];
break;
case State32:
state = table32[bytes[i]];
break;
case State33:
state = table33[bytes[i]];
break;
case State34:
state = table34[bytes[i]];
break;
case State35:
state = table35[bytes[i]];
break;
case State36:
state = table36[bytes[i]];
break;
case State37:
state = table37[bytes[i]];
break;
case State38:
state = table38[bytes[i]];
break;
case State39:
state = table39[bytes[i]];
break;
case State40:
state = table40[bytes[i]];
break;
case State41:
state = table41[bytes[i]];
break;
case State42:
state = table42[bytes[i]];
break;
case State43:
state = table43[bytes[i]];
break;
case State44:
state = table44[bytes[i]];
break;
case State45:
state = table45[bytes[i]];
break;
case State46:
state = table46[bytes[i]];
break;
case State47:
state = table47[bytes[i]];
break;
case State48:
state = table48[bytes[i]];
break;
case State49:
state = table49[bytes[i]];
break;
case State50:
state = table50[bytes[i]];
break;
case State51:
state = table51[bytes[i]];
break;
case State52:
state = table52[bytes[i]];
break;
case State53:
state = table53[bytes[i]];
break;
case State54:
state = table54[bytes[i]];
break;
case State55:
state = table55[bytes[i]];
break;
case State56:
state = table56[bytes[i]];
break;
case State57:
state = table57[bytes[i]];
break;
case State58:
state = table58[bytes[i]];
break;
case State59:
state = table59[bytes[i]];
break;
case State60:
state = table60[bytes[i]];
break;
case State61:
state = table61[bytes[i]];
break;
case State62:
state = table62[bytes[i]];
break;
case State63:
state = table63[bytes[i]];
break;
case State64:
state = table64[bytes[i]];
break;
case State65:
state = table65[bytes[i]];
break;
case State66:
state = table66[bytes[i]];
break;
case State67:
state = table67[bytes[i]];
break;
case State68:
state = table68[bytes[i]];
break;
case State69:
state = table69[bytes[i]];
break;
case State70:
state = table70[bytes[i]];
break;
case State71:
state = table71[bytes[i]];
break;
case State72:
state = table72[bytes[i]];
break;
case State73:
state = table73[bytes[i]];
break;
case State74:
state = table74[bytes[i]];
break;
case State75:
state = table75[bytes[i]];
break;
case State76:
state = table76[bytes[i]];
break;
case State77:
state = table77[bytes[i]];
break;
case State78:
state = table78[bytes[i]];
break;
case State79:
state = table79[bytes[i]];
break;
case State80:
state = table80[bytes[i]];
break;
case State81:
state = table81[bytes[i]];
break;
case State82:
state = table82[bytes[i]];
break;
case State83:
state = table83[bytes[i]];
break;
case State84:
state = table84[bytes[i]];
break;
case State85:
state = table85[bytes[i]];
break;
case State86:
state = table86[bytes[i]];
break;
case State87:
state = table87[bytes[i]];
break;
case State88:
state = table88[bytes[i]];
break;
case State89:
state = table89[bytes[i]];
break;
case State90:
state = table90[bytes[i]];
break;
case State91:
state = table91[bytes[i]];
break;
case State92:
state = table92[bytes[i]];
break;
case State93:
state = table93[bytes[i]];
break;
case State94:
state = table94[bytes[i]];
break;
case State95:
state = table95[bytes[i]];
break;
case State96:
state = table96[bytes[i]];
break;
case State97:
state = table97[bytes[i]];
break;
case State98:
state = table98[bytes[i]];
break;
case State99:
state = table99[bytes[i]];
break;
case State100:
state = table100[bytes[i]];
break;
case State101:
state = table101[bytes[i]];
break;
case State102:
state = table102[bytes[i]];
break;
case State103:
state = table103[bytes[i]];
break;
case State104:
state = table104[bytes[i]];
break;
case State105:
state = table105[bytes[i]];
break;
case State106:
state = table106[bytes[i]];
break;
case State107:
state = table107[bytes[i]];
break;
case State108:
state = table108[bytes[i]];
break;
case State109:
state = table109[bytes[i]];
break;
case State110:
state = table110[bytes[i]];
break;
case State111:
state = table111[bytes[i]];
break;
case State112:
state = table112[bytes[i]];
break;
case State113:
state = table113[bytes[i]];
break;
case State114:
state = table114[bytes[i]];
break;
case State115:
state = table115[bytes[i]];
break;
case State116:
state = table116[bytes[i]];
break;
case State117:
state = table117[bytes[i]];
break;
case State118:
state = table118[bytes[i]];
break;
case State119:
state = table119[bytes[i]];
break;
case State120:
state = table120[bytes[i]];
break;
case State121:
state = table121[bytes[i]];
break;
case State122:
state = table122[bytes[i]];
break;
case State123:
state = table123[bytes[i]];
break;
case State124:
state = table124[bytes[i]];
break;
case State125:
state = table125[bytes[i]];
break;
case State126:
state = table126[bytes[i]];
break;
case State127:
state = table127[bytes[i]];
break;
case State128:
state = table128[bytes[i]];
break;
case State129:
state = table129[bytes[i]];
break;
case State130:
state = table130[bytes[i]];
break;
case State131:
state = table131[bytes[i]];
break;
case State132:
state = table132[bytes[i]];
break;
case State133:
state = table133[bytes[i]];
break;
case State134:
state = table134[bytes[i]];
break;
case State135:
state = table135[bytes[i]];
break;
case State136:
state = table136[bytes[i]];
break;
case State137:
state = table137[bytes[i]];
break;
case State138:
state = table138[bytes[i]];
break;
case State139:
state = table139[bytes[i]];
break;
case State140:
state = table140[bytes[i]];
break;
case State141:
state = table141[bytes[i]];
break;
case State142:
state = table142[bytes[i]];
break;
case State143:
state = table143[bytes[i]];
break;
case State144:
state = table144[bytes[i]];
break;
case State145:
state = table145[bytes[i]];
break;
case State146:
state = table146[bytes[i]];
break;
case State147:
state = table147[bytes[i]];
break;
case State148:
state = table148[bytes[i]];
break;
case State149:
state = table149[bytes[i]];
break;
case State150:
state = table150[bytes[i]];
break;
case State151:
state = table151[bytes[i]];
break;
case State152:
state = table152[bytes[i]];
break;
case State153:
state = table153[bytes[i]];
break;
case State154:
state = table154[bytes[i]];
break;
case State155:
state = table155[bytes[i]];
break;
case State156:
state = table156[bytes[i]];
break;
case State157:
state = table157[bytes[i]];
break;
case State158:
state = table158[bytes[i]];
break;
case State159:
state = table159[bytes[i]];
break;
case State160:
state = table160[bytes[i]];
break;
case State161:
state = table161[bytes[i]];
break;
case State162:
state = table162[bytes[i]];
break;
case State163:
state = table163[bytes[i]];
break;
case State164:
state = table164[bytes[i]];
break;
case State165:
state = table165[bytes[i]];
break;
case State166:
state = table166[bytes[i]];
break;
case State167:
state = table167[bytes[i]];
break;
case State168:
state = table168[bytes[i]];
break;
case State169:
state = table169[bytes[i]];
break;
case State170:
state = table170[bytes[i]];
break;
case State171:
state = table171[bytes[i]];
break;
case State172:
state = table172[bytes[i]];
break;
case State173:
state = table173[bytes[i]];
break;
case State174:
state = table174[bytes[i]];
break;
case State175:
state = table175[bytes[i]];
break;
case State176:
state = table176[bytes[i]];
break;
case State177:
state = table177[bytes[i]];
break;
case State178:
state = table178[bytes[i]];
break;
case State179:
state = table179[bytes[i]];
break;
case State180:
state = table180[bytes[i]];
break;
case State181:
state = table181[bytes[i]];
break;
case State182:
state = table182[bytes[i]];
break;
case State183:
state = table183[bytes[i]];
break;
case State184:
state = table184[bytes[i]];
break;
case State185:
state = table185[bytes[i]];
break;
case State186:
state = table186[bytes[i]];
break;
case State187:
state = table187[bytes[i]];
break;
case State188:
state = table188[bytes[i]];
break;
case State189:
state = table189[bytes[i]];
break;
case State190:
state = table190[bytes[i]];
break;
case State191:
state = table191[bytes[i]];
break;
case State192:
state = table192[bytes[i]];
break;
case State193:
state = table193[bytes[i]];
break;
case State194:
state = table194[bytes[i]];
break;
case State195:
state = table195[bytes[i]];
break;
case State196:
state = table196[bytes[i]];
break;
case State197:
state = table197[bytes[i]];
break;
case State198:
state = table198[bytes[i]];
break;
case State199:
state = table199[bytes[i]];
break;
case State200:
state = table200[bytes[i]];
break;
case State201:
state = table201[bytes[i]];
break;
case State202:
state = table202[bytes[i]];
break;
case State203:
state = table203[bytes[i]];
break;
case State204:
state = table204[bytes[i]];
break;
case State205:
state = table205[bytes[i]];
break;
case State206:
state = table206[bytes[i]];
break;
case State207:
state = table207[bytes[i]];
break;
case State208:
state = table208[bytes[i]];
break;
case State209:
state = table209[bytes[i]];
break;
case State210:
state = table210[bytes[i]];
break;
case State211:
state = table211[bytes[i]];
break;
case State212:
state = table212[bytes[i]];
break;
case State213:
state = table213[bytes[i]];
break;
case State214:
state = table214[bytes[i]];
break;
case State215:
state = table215[bytes[i]];
break;
case State216:
state = table216[bytes[i]];
break;
case State217:
state = table217[bytes[i]];
break;
case State218:
state = table218[bytes[i]];
break;
case State219:
state = table219[bytes[i]];
break;
case State220:
state = table220[bytes[i]];
break;
case State221:
state = table221[bytes[i]];
break;
case State222:
state = table222[bytes[i]];
break;
case State223:
state = table223[bytes[i]];
break;
case State224:
state = table224[bytes[i]];
break;
case State225:
state = table225[bytes[i]];
break;
case State226:
state = table226[bytes[i]];
break;
case State227:
state = table227[bytes[i]];
break;
case State228:
state = table228[bytes[i]];
break;
case State229:
state = table229[bytes[i]];
break;
case State230:
state = table230[bytes[i]];
break;
case State231:
state = table231[bytes[i]];
break;
case State232:
state = table232[bytes[i]];
break;
case State233:
state = table233[bytes[i]];
break;
case State234:
state = table234[bytes[i]];
break;
case State235:
state = table235[bytes[i]];
break;
case State236:
state = table236[bytes[i]];
break;
case State237:
state = table237[bytes[i]];
break;
case State238:
state = table238[bytes[i]];
break;
case State239:
state = table239[bytes[i]];
break;
case State240:
state = table240[bytes[i]];
break;
case State241:
state = table241[bytes[i]];
break;
case State242:
state = table242[bytes[i]];
break;
case State243:
state = table243[bytes[i]];
break;
case State244:
state = table244[bytes[i]];
break;
case State245:
state = table245[bytes[i]];
break;
case State246:
state = table246[bytes[i]];
break;
case State247:
state = table247[bytes[i]];
break;
case State248:
state = table248[bytes[i]];
break;
case State249:
state = table249[bytes[i]];
break;
case State250:
state = table250[bytes[i]];
break;
case State251:
state = table251[bytes[i]];
break;
case State252:
state = table252[bytes[i]];
break;
case State253:
state = table253[bytes[i]];
break;
case State254:
state = table254[bytes[i]];
break;
case State255:
state = table255[bytes[i]];
break;
case State256:
state = table256[bytes[i]];
break;
case State257:
state = table257[bytes[i]];
break;
case State258:
state = table258[bytes[i]];
break;
case State259:
state = table259[bytes[i]];
break;
case State260:
state = table260[bytes[i]];
break;
case State261:
state = table261[bytes[i]];
break;
case State262:
state = table262[bytes[i]];
break;
case State263:
state = table263[bytes[i]];
break;
case State264:
state = table264[bytes[i]];
break;
case State265:
state = table265[bytes[i]];
break;
case State266:
state = table266[bytes[i]];
break;
case State267:
state = table267[bytes[i]];
break;
case State268:
state = table268[bytes[i]];
break;
case State269:
state = table269[bytes[i]];
break;
case State270:
state = table270[bytes[i]];
break;
case State271:
state = table271[bytes[i]];
break;
case State272:
state = table272[bytes[i]];
break;
case State273:
state = table273[bytes[i]];
break;
case State274:
state = table274[bytes[i]];
break;
case State275:
state = table275[bytes[i]];
break;
case State276:
state = table276[bytes[i]];
break;
case State277:
state = table277[bytes[i]];
break;
case State278:
state = table278[bytes[i]];
break;
case State279:
Error = true;
goto exit1;
}
i++;
int end = offset + length;
for( ; i < end; i++)
{
switch(state)
{
case State0:
state = table0[bytes[i]];
break;
case State1:
state = table1[bytes[i]];
break;
case State2:
state = table2[bytes[i]];
break;
case State3:
state = table3[bytes[i]];
break;
case State4:
state = table4[bytes[i]];
break;
case State5:
state = table5[bytes[i]];
break;
case State6:
state = table6[bytes[i]];
break;
case State7:
state = table7[bytes[i]];
break;
case State8:
state = table8[bytes[i]];
break;
case State9:
state = table9[bytes[i]];
break;
case State10:
state = table10[bytes[i]];
break;
case State11:
state = table11[bytes[i]];
break;
case State12:
state = table12[bytes[i]];
break;
case State13:
state = table13[bytes[i]];
break;
case State14:
state = table14[bytes[i]];
break;
case State15:
state = table15[bytes[i]];
break;
case State16:
state = table16[bytes[i]];
break;
case State17:
state = table17[bytes[i]];
break;
case State18:
state = table18[bytes[i]];
break;
case State19:
state = table19[bytes[i]];
break;
case State20:
Final = true;
BaseAction = BaseActions.Role;
goto exit1;
case State21:
state = table21[bytes[i]];
break;
case State22:
state = table22[bytes[i]];
break;
case State23:
state = table23[bytes[i]];
break;
case State24:
state = table24[bytes[i]];
break;
case State25:
state = table25[bytes[i]];
break;
case State26:
state = table26[bytes[i]];
break;
case State27:
state = table27[bytes[i]];
break;
case State28:
state = table28[bytes[i]];
break;
case State29:
state = table29[bytes[i]];
break;
case State30:
state = table30[bytes[i]];
break;
case State31:
state = table31[bytes[i]];
break;
case State32:
state = table32[bytes[i]];
break;
case State33:
state = table33[bytes[i]];
break;
case State34:
state = table34[bytes[i]];
break;
case State35:
Final = true;
BaseAction = BaseActions.Options;
goto exit1;
case State36:
state = table36[bytes[i]];
break;
case State37:
state = table37[bytes[i]];
break;
case State38:
state = table38[bytes[i]];
break;
case State39:
state = table39[bytes[i]];
break;
case State40:
state = table40[bytes[i]];
break;
case State41:
state = table41[bytes[i]];
break;
case State42:
Final = true;
BaseAction = BaseActions.Version;
goto exit1;
case State43:
Final = true;
BaseAction = BaseActions.Accounts;
goto exit1;
case State44:
state = table44[bytes[i]];
break;
case State45:
state = table45[bytes[i]];
break;
case State46:
state = table46[bytes[i]];
break;
case State47:
state = table47[bytes[i]];
break;
case State48:
state = table48[bytes[i]];
break;
case State49:
state = table49[bytes[i]];
break;
case State50:
if(DomainName.Begin < 0)DomainName.Begin = i;
state = table50[bytes[i]];
break;
case State51:
state = table51[bytes[i]];
break;
case State52:
state = table52[bytes[i]];
break;
case State53:
if(Signature.Begin < 0)Signature.Begin = i;
state = table53[bytes[i]];
break;
case State54:
state = table54[bytes[i]];
break;
case State55:
state = table55[bytes[i]];
break;
case State56:
state = table56[bytes[i]];
break;
case State57:
DomainName.End = i;
Final = true;
goto exit1;
case State58:
state = table58[bytes[i]];
break;
case State59:
state = table59[bytes[i]];
break;
case State60:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table60[bytes[i]];
break;
case State61:
state = table61[bytes[i]];
break;
case State62:
state = table62[bytes[i]];
break;
case State63:
if(Authname.Begin < 0)Authname.Begin = i;
state = table63[bytes[i]];
break;
case State64:
DomainName.End = i;
Final = true;
goto exit1;
case State65:
state = table65[bytes[i]];
break;
case State66:
state = table66[bytes[i]];
break;
case State67:
state = table67[bytes[i]];
break;
case State68:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table68[bytes[i]];
break;
case State69:
state = table69[bytes[i]];
break;
case State70:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State71:
Authname.End = i;
Final = true;
goto exit1;
case State72:
DomainName.End = i;
Final = true;
goto exit1;
case State73:
state = table73[bytes[i]];
break;
case State74:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State75:
state = table75[bytes[i]];
break;
case State76:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table76[bytes[i]];
break;
case State77:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State78:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State79:
Authname.End = i;
Final = true;
goto exit1;
case State80:
DomainName.End = i;
Final = true;
goto exit1;
case State81:
state = table81[bytes[i]];
break;
case State82:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State83:
state = table83[bytes[i]];
break;
case State84:
state = table84[bytes[i]];
break;
case State85:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table85[bytes[i]];
break;
case State86:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State87:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State88:
Authname.End = i;
Final = true;
goto exit1;
case State89:
DomainName.End = i;
Final = true;
goto exit1;
case State90:
state = table90[bytes[i]];
break;
case State91:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State92:
state = table92[bytes[i]];
break;
case State93:
state = table93[bytes[i]];
break;
case State94:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table94[bytes[i]];
break;
case State95:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State96:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State97:
Authname.End = i;
Final = true;
goto exit1;
case State98:
DomainName.End = i;
Final = true;
goto exit1;
case State99:
state = table99[bytes[i]];
break;
case State100:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State101:
state = table101[bytes[i]];
break;
case State102:
Final = true;
Method = Methods.Put;
goto exit1;
case State103:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table103[bytes[i]];
break;
case State104:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State105:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State106:
Authname.End = i;
Final = true;
goto exit1;
case State107:
DomainName.End = i;
Final = true;
goto exit1;
case State108:
Final = true;
AccountAction = AccountActions.Userz;
goto exit1;
case State109:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State110:
state = table110[bytes[i]];
break;
case State111:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table111[bytes[i]];
break;
case State112:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State113:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State114:
Authname.End = i;
Final = true;
goto exit1;
case State115:
DomainName.End = i;
Final = true;
goto exit1;
case State116:
if(UsersId.Begin < 0)UsersId.Begin = i;
state = table116[bytes[i]];
break;
case State117:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State118:
state = table118[bytes[i]];
break;
case State119:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table119[bytes[i]];
break;
case State120:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State121:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State122:
Authname.End = i;
Final = true;
goto exit1;
case State123:
DomainName.End = i;
Final = true;
goto exit1;
case State124:
UsersId.End = i;
Final = true;
goto exit1;
case State125:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State126:
Final = true;
Method = Methods.Delete;
goto exit1;
case State127:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table127[bytes[i]];
break;
case State128:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State129:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State130:
Authname.End = i;
Final = true;
goto exit1;
case State131:
DomainName.End = i;
Final = true;
goto exit1;
case State132:
if(Username.Begin < 0)Username.Begin = i;
state = table132[bytes[i]];
break;
case State133:
UsersId.End = i;
Final = true;
goto exit1;
case State134:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State135:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table135[bytes[i]];
break;
case State136:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State137:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State138:
Authname.End = i;
Final = true;
goto exit1;
case State139:
DomainName.End = i;
Final = true;
goto exit1;
case State140:
Username.End = i;
Final = true;
goto exit1;
case State141:
UsersId.End = i;
Final = true;
goto exit1;
case State142:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table142[bytes[i]];
break;
case State143:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State144:
Authname.End = i;
Final = true;
goto exit1;
case State145:
DomainName.End = i;
Final = true;
goto exit1;
case State146:
Username.End = i;
Final = true;
goto exit1;
case State147:
UsersId.End = i;
Final = true;
goto exit1;
case State148:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table148[bytes[i]];
break;
case State149:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State150:
Authname.End = i;
Final = true;
goto exit1;
case State151:
DomainName.End = i;
Final = true;
goto exit1;
case State152:
Username.End = i;
Final = true;
goto exit1;
case State153:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table153[bytes[i]];
break;
case State154:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State155:
Authname.End = i;
Final = true;
goto exit1;
case State156:
DomainName.End = i;
Final = true;
goto exit1;
case State157:
Username.End = i;
Final = true;
goto exit1;
case State158:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table158[bytes[i]];
break;
case State159:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State160:
Authname.End = i;
Final = true;
goto exit1;
case State161:
DomainName.End = i;
Final = true;
goto exit1;
case State162:
Username.End = i;
Final = true;
goto exit1;
case State163:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table163[bytes[i]];
break;
case State164:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State165:
Authname.End = i;
Final = true;
goto exit1;
case State166:
DomainName.End = i;
Final = true;
goto exit1;
case State167:
Username.End = i;
Final = true;
goto exit1;
case State168:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table168[bytes[i]];
break;
case State169:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State170:
Authname.End = i;
Final = true;
goto exit1;
case State171:
DomainName.End = i;
Final = true;
goto exit1;
case State172:
Username.End = i;
Final = true;
goto exit1;
case State173:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table173[bytes[i]];
break;
case State174:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State175:
Authname.End = i;
Final = true;
goto exit1;
case State176:
DomainName.End = i;
Final = true;
goto exit1;
case State177:
Username.End = i;
Final = true;
goto exit1;
case State178:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table178[bytes[i]];
break;
case State179:
Authname.End = i;
Final = true;
goto exit1;
case State180:
DomainName.End = i;
Final = true;
goto exit1;
case State181:
Username.End = i;
Final = true;
goto exit1;
case State182:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table182[bytes[i]];
break;
case State183:
Authname.End = i;
Final = true;
goto exit1;
case State184:
DomainName.End = i;
Final = true;
goto exit1;
case State185:
Username.End = i;
Final = true;
goto exit1;
case State186:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table186[bytes[i]];
break;
case State187:
Authname.End = i;
Final = true;
goto exit1;
case State188:
DomainName.End = i;
Final = true;
goto exit1;
case State189:
Username.End = i;
Final = true;
goto exit1;
case State190:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table190[bytes[i]];
break;
case State191:
Authname.End = i;
Final = true;
goto exit1;
case State192:
DomainName.End = i;
Final = true;
goto exit1;
case State193:
Username.End = i;
Final = true;
goto exit1;
case State194:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table194[bytes[i]];
break;
case State195:
Authname.End = i;
Final = true;
goto exit1;
case State196:
DomainName.End = i;
Final = true;
goto exit1;
case State197:
Username.End = i;
Final = true;
goto exit1;
case State198:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table198[bytes[i]];
break;
case State199:
Authname.End = i;
Final = true;
goto exit1;
case State200:
DomainName.End = i;
Final = true;
goto exit1;
case State201:
Username.End = i;
Final = true;
goto exit1;
case State202:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table202[bytes[i]];
break;
case State203:
Authname.End = i;
Final = true;
goto exit1;
case State204:
DomainName.End = i;
Final = true;
goto exit1;
case State205:
Username.End = i;
Final = true;
goto exit1;
case State206:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table206[bytes[i]];
break;
case State207:
Authname.End = i;
Final = true;
goto exit1;
case State208:
DomainName.End = i;
Final = true;
goto exit1;
case State209:
Username.End = i;
Final = true;
goto exit1;
case State210:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table210[bytes[i]];
break;
case State211:
Authname.End = i;
Final = true;
goto exit1;
case State212:
DomainName.End = i;
Final = true;
goto exit1;
case State213:
Username.End = i;
Final = true;
goto exit1;
case State214:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table214[bytes[i]];
break;
case State215:
Authname.End = i;
Final = true;
goto exit1;
case State216:
DomainName.End = i;
Final = true;
goto exit1;
case State217:
Username.End = i;
Final = true;
goto exit1;
case State218:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table218[bytes[i]];
break;
case State219:
Authname.End = i;
Final = true;
goto exit1;
case State220:
DomainName.End = i;
Final = true;
goto exit1;
case State221:
Username.End = i;
Final = true;
goto exit1;
case State222:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table222[bytes[i]];
break;
case State223:
Authname.End = i;
Final = true;
goto exit1;
case State224:
DomainName.End = i;
Final = true;
goto exit1;
case State225:
Username.End = i;
Final = true;
goto exit1;
case State226:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table226[bytes[i]];
break;
case State227:
Authname.End = i;
Final = true;
goto exit1;
case State228:
DomainName.End = i;
Final = true;
goto exit1;
case State229:
Username.End = i;
Final = true;
goto exit1;
case State230:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
state = table230[bytes[i]];
break;
case State231:
Authname.End = i;
Final = true;
goto exit1;
case State232:
DomainName.End = i;
Final = true;
goto exit1;
case State233:
Username.End = i;
Final = true;
goto exit1;
case State234:
Signature.End = i;
Final = true;
goto exit1;
case State235:
Authname.End = i;
Final = true;
goto exit1;
case State236:
DomainName.End = i;
Final = true;
goto exit1;
case State237:
Username.End = i;
Final = true;
goto exit1;
case State238:
Authname.End = i;
Final = true;
goto exit1;
case State239:
DomainName.End = i;
Final = true;
goto exit1;
case State240:
Username.End = i;
Final = true;
goto exit1;
case State241:
DomainName.End = i;
Final = true;
goto exit1;
case State242:
Username.End = i;
Final = true;
goto exit1;
case State243:
DomainName.End = i;
Final = true;
goto exit1;
case State244:
Username.End = i;
Final = true;
goto exit1;
case State245:
DomainName.End = i;
Final = true;
goto exit1;
case State246:
Username.End = i;
Final = true;
goto exit1;
case State247:
DomainName.End = i;
Final = true;
goto exit1;
case State248:
Username.End = i;
Final = true;
goto exit1;
case State249:
DomainName.End = i;
Final = true;
goto exit1;
case State250:
Username.End = i;
Final = true;
goto exit1;
case State251:
DomainName.End = i;
Final = true;
goto exit1;
case State252:
Username.End = i;
Final = true;
goto exit1;
case State253:
DomainName.End = i;
Final = true;
goto exit1;
case State254:
Username.End = i;
Final = true;
goto exit1;
case State255:
DomainName.End = i;
Final = true;
goto exit1;
case State256:
Username.End = i;
Final = true;
goto exit1;
case State257:
DomainName.End = i;
Final = true;
goto exit1;
case State258:
DomainName.End = i;
Final = true;
goto exit1;
case State259:
DomainName.End = i;
Final = true;
goto exit1;
case State260:
DomainName.End = i;
Final = true;
goto exit1;
case State261:
DomainName.End = i;
Final = true;
goto exit1;
case State262:
DomainName.End = i;
Final = true;
goto exit1;
case State263:
DomainName.End = i;
Final = true;
goto exit1;
case State264:
DomainName.End = i;
Final = true;
goto exit1;
case State265:
DomainName.End = i;
Final = true;
goto exit1;
case State266:
DomainName.End = i;
Final = true;
goto exit1;
case State267:
DomainName.End = i;
Final = true;
goto exit1;
case State268:
DomainName.End = i;
Final = true;
goto exit1;
case State269:
DomainName.End = i;
Final = true;
goto exit1;
case State270:
DomainName.End = i;
Final = true;
goto exit1;
case State271:
DomainName.End = i;
Final = true;
goto exit1;
case State272:
DomainName.End = i;
Final = true;
goto exit1;
case State273:
DomainName.End = i;
Final = true;
goto exit1;
case State274:
DomainName.End = i;
Final = true;
goto exit1;
case State275:
DomainName.End = i;
Final = true;
goto exit1;
case State276:
DomainName.End = i;
Final = true;
goto exit1;
case State277:
DomainName.End = i;
Final = true;
goto exit1;
case State278:
DomainName.End = i;
Final = true;
goto exit1;
case State279:
i--;
Error = true;
goto exit1;
}
}
switch(state)
{
case State0:
break;
case State1:
break;
case State2:
break;
case State3:
break;
case State4:
break;
case State5:
break;
case State6:
break;
case State7:
break;
case State8:
break;
case State9:
break;
case State10:
break;
case State11:
break;
case State12:
break;
case State13:
break;
case State14:
break;
case State15:
break;
case State16:
break;
case State17:
break;
case State18:
break;
case State19:
break;
case State20:
Final = true;
BaseAction = BaseActions.Role;
goto exit1;
case State21:
break;
case State22:
break;
case State23:
break;
case State24:
break;
case State25:
break;
case State26:
break;
case State27:
break;
case State28:
break;
case State29:
break;
case State30:
break;
case State31:
break;
case State32:
break;
case State33:
break;
case State34:
break;
case State35:
Final = true;
BaseAction = BaseActions.Options;
goto exit1;
case State36:
break;
case State37:
break;
case State38:
break;
case State39:
break;
case State40:
break;
case State41:
break;
case State42:
Final = true;
BaseAction = BaseActions.Version;
goto exit1;
case State43:
Final = true;
BaseAction = BaseActions.Accounts;
goto exit1;
case State44:
break;
case State45:
break;
case State46:
break;
case State47:
break;
case State48:
break;
case State49:
break;
case State50:
if(DomainName.Begin < 0)DomainName.Begin = i;
break;
case State51:
break;
case State52:
break;
case State53:
if(Signature.Begin < 0)Signature.Begin = i;
break;
case State54:
break;
case State55:
break;
case State56:
break;
case State57:
DomainName.End = i;
Final = true;
goto exit1;
case State58:
break;
case State59:
break;
case State60:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State61:
break;
case State62:
break;
case State63:
if(Authname.Begin < 0)Authname.Begin = i;
break;
case State64:
DomainName.End = i;
Final = true;
goto exit1;
case State65:
break;
case State66:
break;
case State67:
break;
case State68:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State69:
break;
case State70:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State71:
Authname.End = i;
Final = true;
goto exit1;
case State72:
DomainName.End = i;
Final = true;
goto exit1;
case State73:
break;
case State74:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State75:
break;
case State76:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State77:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State78:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State79:
Authname.End = i;
Final = true;
goto exit1;
case State80:
DomainName.End = i;
Final = true;
goto exit1;
case State81:
break;
case State82:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State83:
break;
case State84:
break;
case State85:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State86:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State87:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State88:
Authname.End = i;
Final = true;
goto exit1;
case State89:
DomainName.End = i;
Final = true;
goto exit1;
case State90:
break;
case State91:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State92:
break;
case State93:
break;
case State94:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State95:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State96:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State97:
Authname.End = i;
Final = true;
goto exit1;
case State98:
DomainName.End = i;
Final = true;
goto exit1;
case State99:
break;
case State100:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State101:
break;
case State102:
Final = true;
Method = Methods.Put;
goto exit1;
case State103:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State104:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State105:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State106:
Authname.End = i;
Final = true;
goto exit1;
case State107:
DomainName.End = i;
Final = true;
goto exit1;
case State108:
Final = true;
AccountAction = AccountActions.Userz;
goto exit1;
case State109:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State110:
break;
case State111:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State112:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State113:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State114:
Authname.End = i;
Final = true;
goto exit1;
case State115:
DomainName.End = i;
Final = true;
goto exit1;
case State116:
if(UsersId.Begin < 0)UsersId.Begin = i;
break;
case State117:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State118:
break;
case State119:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State120:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State121:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State122:
Authname.End = i;
Final = true;
goto exit1;
case State123:
DomainName.End = i;
Final = true;
goto exit1;
case State124:
UsersId.End = i;
Final = true;
goto exit1;
case State125:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State126:
Final = true;
Method = Methods.Delete;
goto exit1;
case State127:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State128:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State129:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State130:
Authname.End = i;
Final = true;
goto exit1;
case State131:
DomainName.End = i;
Final = true;
goto exit1;
case State132:
if(Username.Begin < 0)Username.Begin = i;
break;
case State133:
UsersId.End = i;
Final = true;
goto exit1;
case State134:
Final = true;
Count = (Count << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State135:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State136:
Final = true;
StartIndex = (StartIndex << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State137:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State138:
Authname.End = i;
Final = true;
goto exit1;
case State139:
DomainName.End = i;
Final = true;
goto exit1;
case State140:
Username.End = i;
Final = true;
goto exit1;
case State141:
UsersId.End = i;
Final = true;
goto exit1;
case State142:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State143:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State144:
Authname.End = i;
Final = true;
goto exit1;
case State145:
DomainName.End = i;
Final = true;
goto exit1;
case State146:
Username.End = i;
Final = true;
goto exit1;
case State147:
UsersId.End = i;
Final = true;
goto exit1;
case State148:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State149:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State150:
Authname.End = i;
Final = true;
goto exit1;
case State151:
DomainName.End = i;
Final = true;
goto exit1;
case State152:
Username.End = i;
Final = true;
goto exit1;
case State153:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State154:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State155:
Authname.End = i;
Final = true;
goto exit1;
case State156:
DomainName.End = i;
Final = true;
goto exit1;
case State157:
Username.End = i;
Final = true;
goto exit1;
case State158:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State159:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State160:
Authname.End = i;
Final = true;
goto exit1;
case State161:
DomainName.End = i;
Final = true;
goto exit1;
case State162:
Username.End = i;
Final = true;
goto exit1;
case State163:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State164:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State165:
Authname.End = i;
Final = true;
goto exit1;
case State166:
DomainName.End = i;
Final = true;
goto exit1;
case State167:
Username.End = i;
Final = true;
goto exit1;
case State168:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State169:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State170:
Authname.End = i;
Final = true;
goto exit1;
case State171:
DomainName.End = i;
Final = true;
goto exit1;
case State172:
Username.End = i;
Final = true;
goto exit1;
case State173:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State174:
Final = true;
Timestamp = (Timestamp << 1) * 5 + bytes[i - 1] - 48;
goto exit1;
case State175:
Authname.End = i;
Final = true;
goto exit1;
case State176:
DomainName.End = i;
Final = true;
goto exit1;
case State177:
Username.End = i;
Final = true;
goto exit1;
case State178:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State179:
Authname.End = i;
Final = true;
goto exit1;
case State180:
DomainName.End = i;
Final = true;
goto exit1;
case State181:
Username.End = i;
Final = true;
goto exit1;
case State182:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State183:
Authname.End = i;
Final = true;
goto exit1;
case State184:
DomainName.End = i;
Final = true;
goto exit1;
case State185:
Username.End = i;
Final = true;
goto exit1;
case State186:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State187:
Authname.End = i;
Final = true;
goto exit1;
case State188:
DomainName.End = i;
Final = true;
goto exit1;
case State189:
Username.End = i;
Final = true;
goto exit1;
case State190:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State191:
Authname.End = i;
Final = true;
goto exit1;
case State192:
DomainName.End = i;
Final = true;
goto exit1;
case State193:
Username.End = i;
Final = true;
goto exit1;
case State194:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State195:
Authname.End = i;
Final = true;
goto exit1;
case State196:
DomainName.End = i;
Final = true;
goto exit1;
case State197:
Username.End = i;
Final = true;
goto exit1;
case State198:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State199:
Authname.End = i;
Final = true;
goto exit1;
case State200:
DomainName.End = i;
Final = true;
goto exit1;
case State201:
Username.End = i;
Final = true;
goto exit1;
case State202:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State203:
Authname.End = i;
Final = true;
goto exit1;
case State204:
DomainName.End = i;
Final = true;
goto exit1;
case State205:
Username.End = i;
Final = true;
goto exit1;
case State206:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State207:
Authname.End = i;
Final = true;
goto exit1;
case State208:
DomainName.End = i;
Final = true;
goto exit1;
case State209:
Username.End = i;
Final = true;
goto exit1;
case State210:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State211:
Authname.End = i;
Final = true;
goto exit1;
case State212:
DomainName.End = i;
Final = true;
goto exit1;
case State213:
Username.End = i;
Final = true;
goto exit1;
case State214:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State215:
Authname.End = i;
Final = true;
goto exit1;
case State216:
DomainName.End = i;
Final = true;
goto exit1;
case State217:
Username.End = i;
Final = true;
goto exit1;
case State218:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State219:
Authname.End = i;
Final = true;
goto exit1;
case State220:
DomainName.End = i;
Final = true;
goto exit1;
case State221:
Username.End = i;
Final = true;
goto exit1;
case State222:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State223:
Authname.End = i;
Final = true;
goto exit1;
case State224:
DomainName.End = i;
Final = true;
goto exit1;
case State225:
Username.End = i;
Final = true;
goto exit1;
case State226:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State227:
Authname.End = i;
Final = true;
goto exit1;
case State228:
DomainName.End = i;
Final = true;
goto exit1;
case State229:
Username.End = i;
Final = true;
goto exit1;
case State230:
if(Signature.Begin < 0)Signature.Begin = i;
Signature.End = i;
break;
case State231:
Authname.End = i;
Final = true;
goto exit1;
case State232:
DomainName.End = i;
Final = true;
goto exit1;
case State233:
Username.End = i;
Final = true;
goto exit1;
case State234:
Signature.End = i;
Final = true;
goto exit1;
case State235:
Authname.End = i;
Final = true;
goto exit1;
case State236:
DomainName.End = i;
Final = true;
goto exit1;
case State237:
Username.End = i;
Final = true;
goto exit1;
case State238:
Authname.End = i;
Final = true;
goto exit1;
case State239:
DomainName.End = i;
Final = true;
goto exit1;
case State240:
Username.End = i;
Final = true;
goto exit1;
case State241:
DomainName.End = i;
Final = true;
goto exit1;
case State242:
Username.End = i;
Final = true;
goto exit1;
case State243:
DomainName.End = i;
Final = true;
goto exit1;
case State244:
Username.End = i;
Final = true;
goto exit1;
case State245:
DomainName.End = i;
Final = true;
goto exit1;
case State246:
Username.End = i;
Final = true;
goto exit1;
case State247:
DomainName.End = i;
Final = true;
goto exit1;
case State248:
Username.End = i;
Final = true;
goto exit1;
case State249:
DomainName.End = i;
Final = true;
goto exit1;
case State250:
Username.End = i;
Final = true;
goto exit1;
case State251:
DomainName.End = i;
Final = true;
goto exit1;
case State252:
Username.End = i;
Final = true;
goto exit1;
case State253:
DomainName.End = i;
Final = true;
goto exit1;
case State254:
Username.End = i;
Final = true;
goto exit1;
case State255:
DomainName.End = i;
Final = true;
goto exit1;
case State256:
Username.End = i;
Final = true;
goto exit1;
case State257:
DomainName.End = i;
Final = true;
goto exit1;
case State258:
DomainName.End = i;
Final = true;
goto exit1;
case State259:
DomainName.End = i;
Final = true;
goto exit1;
case State260:
DomainName.End = i;
Final = true;
goto exit1;
case State261:
DomainName.End = i;
Final = true;
goto exit1;
case State262:
DomainName.End = i;
Final = true;
goto exit1;
case State263:
DomainName.End = i;
Final = true;
goto exit1;
case State264:
DomainName.End = i;
Final = true;
goto exit1;
case State265:
DomainName.End = i;
Final = true;
goto exit1;
case State266:
DomainName.End = i;
Final = true;
goto exit1;
case State267:
DomainName.End = i;
Final = true;
goto exit1;
case State268:
DomainName.End = i;
Final = true;
goto exit1;
case State269:
DomainName.End = i;
Final = true;
goto exit1;
case State270:
DomainName.End = i;
Final = true;
goto exit1;
case State271:
DomainName.End = i;
Final = true;
goto exit1;
case State272:
DomainName.End = i;
Final = true;
goto exit1;
case State273:
DomainName.End = i;
Final = true;
goto exit1;
case State274:
DomainName.End = i;
Final = true;
goto exit1;
case State275:
DomainName.End = i;
Final = true;
goto exit1;
case State276:
DomainName.End = i;
Final = true;
goto exit1;
case State277:
DomainName.End = i;
Final = true;
goto exit1;
case State278:
DomainName.End = i;
Final = true;
goto exit1;
case State279:
i--;
Error = true;
goto exit1;
}
exit1: ;
OnAfterParse();
return i - offset;
}
#endregion
public static readonly byte[] AsciiCodeToHex = new byte[256] {
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0x00, 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08, 0x09, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0x0a, 0x0b, 0x0c, 0x0d, 0x0e, 0x0f, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff,
};
}
}
#endif
