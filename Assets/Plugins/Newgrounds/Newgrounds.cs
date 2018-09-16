using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Newgrounds : MonoBehaviour {
private Hashtable headers = new Hashtable();
private System.Text.Encoding encoding = new System.Text.UTF8Encoding();
private string rad = "/g8236klvBQ#&|;Zb*7CEA59%s`Oue1wziFp$rDVY@TKxUPWytSaGHJ>dmoMR^<0~4qNLhc(I+fjn)X";
private string HexRad = "0123456789ABCDEF";
private int[] sbox;
private int[] key;
private int n;
private string URL;
string APIID;
string password;
bool  preloadSettings;
string versionNumber;
string DefaultUsername;
//bool  flashPreloader;
string SessionID;
int UserID;
string UserName;
int PublisherId;
public Medal[] Medals = new Medal[0];
    public SaveGroup[] SaveGroups = new SaveGroup[0];
string[] boardNames;
int[] boardIndices;
    public string errorMessage;
public int errorCode;
public bool  success;

private bool  mStarted;
private bool  mWorking;

public class Medal {
	public int id;
	public string name;
	public int points;
	public int difficulty;
	public bool  unlocked;
	public bool  secret;
	public string description;
	public string url;
	public Texture2D icon;

	public Medal ( int i ,   string n ,   int p ,   int d ,   bool u ,    bool s ,    string de ,   string ur  ){
		id = i;
		name = n;
		points = p;
		difficulty = d;
		unlocked = u;
		secret = s;
		description = de;
		url = ur;
}}

public class Rating {
	public int id;
	public string name;
	public int min;
	public int max;
	public bool  isFloat;
	
	public Rating ( int i ,   string n ,   int mi ,   int ma ,   bool fl  ){
		 id = i;
		name = n;
		min = mi;
		max = ma;
		isFloat = fl;
}}

public class Key {
	public int id;
	public string name;
	public string type;
	
	public Key ( int i ,   string n ,   int t  ){
		id = i;
		name = n;
		if (t == 1)
			type = "Float";
		if (t == 2)
			type = "Int";
		if (t == 3)
			type = "String";
		if (t == 4)
			type = "Boolean";
}}

public class SaveGroup {
	public int id;
	public string name;
	public string type;
	public Key[] keys;
	public Rating[] ratings;
	
	public SaveGroup ( int i ,   string n ,   int t ,   Key[] k ,   Rating[] r  ){
		id = i;
		name = n;
		keys = k;
		ratings = r;
		if (t==0)
			type = "System";
		if (t==1)
			type = "Private";
		if (t==2)
			type = "Public";
		if (t==3)
			type = "Moderated";
}}

//Application.ExternalEval(" UnityObject2.instances[0].getUnity().SendMessage('" + name + "','bob', document.location.href);");

public bool HasStarted (){
	return mStarted;
}

    public bool IsWorking (){
	return mWorking;
}

IEnumerator Start (){
	mWorking = true;
	
	string a = "[{\"id\": 316, \"value\":\"23.456f\"}, {\"id\":315, \"value\":\"Google\"}, {\"id\":318, \"value\":\"true\"}, {\"id\":317,\"value\":317}]";
	headers.Add("Content-Type", "application/x-www-form-urlencoded");
	headers.Add("Accept","*/*");
	DontDestroyOnLoad (transform.gameObject);
	yield return new WaitForSeconds (5);
        yield return StartCoroutine(registerSession());
//	if (flashPreloader) {
//		Application.ExternalEval ("hideUnity()");

	mStarted = true;
	mWorking = false;
	
	Debug.Log("Newgrounds Started SessionID: "+SessionID+" UserID: "+UserID+" Name: "+UserName);
}

public bool IsLoggedIn (){
	return SessionID.Length > 0;
}

IEnumerator downloadMedal ( string s  ){
	int m = findMedal (s);
	WWW download = new WWW (Medals[m].url);
	Medals[m].icon = new Texture2D (1,1);
	yield return download;
	download.LoadImageIntoTexture(Medals[m].icon);
}

    IEnumerator downloadAllMedals (){
	for (int i=0; i<Medals.Length; i++) {
		WWW download = new WWW (Medals[i].url);
		Medals[i].icon = new Texture2D (1,1);
		yield return download;
		download.LoadImageIntoTexture(Medals[i].icon);
}}

public int findMedal ( string s  ){
	Debug.Log("Newgrounds Find Medal: "+s);
	for (int i = 0; i < Medals.Length; i++) {
		if (Medals[i].name == s)
			return i;
}	throw new UnityException ("Medal Doesn't Exist");
}

public IEnumerator unlockMedal ( string s  ){
	Debug.Log("Newgrounds Unlock Medal: "+s);

	mWorking = true;

	int a = findMedal(s);
	Medals[a].unlocked = true;
	a = Medals[a].id;
	string seed = genSeed ();
	string text = "{\"command_id\":\"unlockMedal\",\"publisher_id\":" + PublisherId + ",\"session_id\":\"" + SessionID + "\",\"medal_id\":" + a + ",\"seed\":\"" + seed + "\"}";
        yield return StartCoroutine(SecurePacket(seed, text));
			
	mWorking = false;
	Debug.Log("Newgrounds Finish Unlocking Medal: "+s);
}

public IEnumerator postScore ( int score ,   string BoardName  ){
	Debug.Log("Newgrounds submit score to "+BoardName);
	
	mWorking = true;
	
	int BoardId = 0;
	for (int i = 0; i < boardNames.Length; i++){
		if (BoardName == boardNames[i])
			BoardId = boardIndices[i];
}	string seed = genSeed ();
	string text = "{\"command_id\":\"postScore\",\"publisher_id\":" + PublisherId + ",\"session_id\":\"" + SessionID + "\",\"board\":" + BoardId + ",\"value\":" + score + ",\"seed\":\"" + seed + "\"}";
        yield return StartCoroutine(SecurePacket(seed, text));
	
	mWorking = false;
	Debug.Log("Newgrounds Finish Submitting Score: "+BoardName);
}

    public IEnumerator getMedals (){
	mWorking = true;
	
	WWW download = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=getMedals&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&publisher%5Fid=" + PublisherId + "&user%5Fid=" + UserID), headers);
	yield return download;
	
	//Debug.Log(download.text);
			
	mWorking = false;
	Debug.Log("Newgrounds Finish Getting Medals");
	if(!string.IsNullOrEmpty(download.error)) {
		Debug.Log("Newgrounds Error: "+download.error);
		success = false;
	}
	else {
		parseJSON (download.text);
	}
	
	if(Medals.Length > 0) {		
		for(int i = 0; i < Medals.Length; i++) {
			Debug.Log("Medal: "+Medals[i].id+" name: "+Medals[i].name);
		}
	}
	else
		Debug.Log("Empty Medals WTF");
}

void parseJSON ( string str  ){
	bool  left = true;
	string commandId = "";
	string itemName = "";
	string item = "";
	bool  isNum = false;
	int start = 0;
	bool  isString = false;
	
	int ID = 0;
	string name0 = "";
	int type = 0;
	int min = 0;
	int max = 0;
	bool  isFloat = false;
	
	var tempKeys = new Key[0];
	var tempRatings = new Rating[0];
	
	int medalID = 0;
	string medalName = "";
	int medalValue = 0;
	int medalDifficulty = 0;
	bool  medalUnlocked = false;
	bool  secret = false;
	string medalIcon = "";
	string medalDescription = "";
	
	bool  duplicate = false;
	
	bool  scoreBoards = false;
	bool  saveGroups0 = false;
	bool  keys = false;
	bool  ratings = false;
	
	int groupID = 0;
	string groupName = "";
	int groupType = 0;
	
	errorCode = -1;
	errorMessage = "";
	success = true;
	
	if ((str[0] != '{') && (str[1] != '{')) {
		throw new UnityException ("Command Failed: Unknown Command");
}	for (int i = 0; i < str.Length; i++) {
		if (isString) {
			if ((str[i] == '"') && (str[i-1] != '\\')){
				if (left) {
					itemName = str.Substring (start, (i-start));
					if (itemName == "score_boards")
						scoreBoards = true;
					if (itemName == "save_groups")
						saveGroups0 = true;
					if (itemName == "keys") {
						keys = true;
                            tempKeys = new Key[0];
                        }					if (itemName == "ratings") {
						ratings = true;
                            tempRatings = new Rating[0];
                        }
                    }				else {
					item = str.Substring (start, (i-start));
					if (itemName == "command_id")
						commandId = item;
					if (itemName == "medal_name")
						medalName = item;
					if (itemName == "medal_icon"){
						for (int z = 0; z < item.Length; z++){
							if (item[z] == '\\')
								item = (item.Substring (0,z) + item.Substring(z+1));
}						medalIcon = item;
}					if (itemName == "medal_description")
						medalDescription = item;	
					if (itemName == "name")
						name0 = item;	
					if (itemName == "group_name")
						groupName = item;
					if (itemName == "error_msg")
						errorMessage = item;
					
}				isString = false;
}}		else if (str[i] == ':'){
			left = false;
			if (str[i+1] != '"') {
				start = (i+1);
				isNum = true;
}}		else if (str[i] == '"') {
			isString = true;
			start = (i+1);
}		
if(!isString) {
if ((str[i] == ',') || (str[i] == '}')) {
			left = true;
			if (isNum)  {
				item = str.Substring (start, (i-start));
				if (itemName == "medal_id")
					medalID = int.Parse (item);
				if (itemName == "medal_value" )
					medalValue = int.Parse (item);
				if (itemName == "medal_difficulty")
					medalDifficulty = int.Parse (item);
				if (itemName == "id")
					ID = int.Parse (item);
				if (itemName == "type")
					type = int.Parse (item);
				if (itemName == "min")
					min = int.Parse (item);
				if (itemName == "max")
					max = int.Parse (item);
				if (itemName == "group_id")
					groupID = int.Parse (item);
				if (itemName == "group_type")
					groupType = int.Parse (item);
				if (itemName == "secret"){
					if (item == "1" )
						secret = true;
					else
						secret = false;
}				if (itemName == "medal_unlocked" ){
					if (item == "true" )
						medalUnlocked = true;
					else
						medalUnlocked = false;
}				if (itemName == "float" ){
					if (item == "true" )
						isFloat = true;
					else
						isFloat = false;
				if (itemName == "error_code")
					errorCode = int.Parse(item);
}				if ((itemName == "success") && (item == "0"))
					success = false;
					//throw new UnityException ("Command Failed: " + commandId);				
				isNum = false;
}}		if (str[i] == '}') {
			if (medalID != 0){
				for (int j = 0; j < Medals.Length; j++) {
					if (medalID == Medals[j].id)
						duplicate = true;
}				if (!duplicate) {
					Medal[] temp = new Medal[Medals.Length + 1];
					temp[temp.Length - 1] = new Medal(medalID, medalName, medalValue, medalDifficulty, medalUnlocked, secret, medalDescription, medalIcon);
					for(int k = 0; k < Medals.Length; k++) {
						temp[k] = Medals[k];
}					Medals = temp;
				medalID = 0;
}}			else if (scoreBoards) {
				for (int l = 0; l < boardIndices.Length; l++) {
					if (ID == boardIndices[l])
						duplicate = true;
}				if (!duplicate) {
					string[] temp0 = new string[boardNames.Length + 1];
					int[] temp1 = new int[boardIndices.Length + 1];
					temp0[temp0.Length - 1] = name0;
					temp1[temp1.Length - 1] = ID;
					for(int m = 0; m < boardNames.Length; m++) {
						temp0[m] = boardNames[m];
						temp1[m] = boardIndices[m];
}					boardNames = temp0;
					boardIndices = temp1;
}}			else if (keys) {
				Key[] temp2 = new Key[tempKeys.Length + 1];
				temp2[temp2.Length-1] = new Key(ID, name0, type);
				for (int o = 0; o < tempKeys.Length; o++) 
					temp2[o] = tempKeys[o];
				tempKeys = temp2;
}			else if (ratings) {
				Rating[] temp3 = new Rating[tempRatings.Length + 1];
				temp3[temp3.Length-1] = new Rating(ID, name0, min, max, isFloat);
				for (int p = 0; p < tempRatings.Length; p++) 
					temp3[p] = tempRatings[p];
				tempRatings = temp3;
}			else if (saveGroups0){
				for (int q = 0; q < SaveGroups.Length; q++) {
					if (groupID == SaveGroups[q].id)
						duplicate = true;
}				if (!duplicate) {
					SaveGroup[] temp4 = new SaveGroup[SaveGroups.Length + 1];
					temp4[temp4.Length - 1] = new SaveGroup(groupID, groupName, groupType, tempKeys, tempRatings);
					for(int r = 0; r < SaveGroups.Length; r++) {
						temp4[r] = SaveGroups[r];
}					SaveGroups = temp4;
}}			duplicate = false;
}		else if (str[i] == '{') 
			left = true;
		else if (str[i] == ']') {
			scoreBoards = false;
			if (!keys && !ratings)
				saveGroups0 = false;
			keys = false;
			ratings = false;
}}}

	if(!success && errorMessage.Length == 0)
		errorMessage = commandId;

}

public IEnumerator loadSettings (){
    mWorking = true;

	WWW download = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=preloadSettings&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&publisher%5Fid=" + PublisherId + "&user%5Fid=" + UserID), headers);
	yield return download;
	
	mWorking = false;
	Debug.Log("Newgrounds Finish Getting Settings");
	if(!string.IsNullOrEmpty(download.error)) {
		Debug.Log("Newgrounds Error: "+download.error);
		success = false;
	}
	else {
		Debug.Log("Newgrounds Settings: "+download.text);
		parseJSON (download.text);
	}
	
	if(Medals.Length > 0) {		
		for(int i = 0; i < Medals.Length; i++) {
			Debug.Log("Medal: "+Medals[i].id+" name: "+Medals[i].name);
		}
	}
	
	parseJSON (download.text);
}

IEnumerator registerSession (){
	string a = "";
	if (preloadSettings)
		a = "&preload=1";
	WWW download = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=connectMovie&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&publisher%5Fid=" + PublisherId + "&user%5Fid=" + UserID + "&host=" + URL + a +"&movie_version=" + versionNumber), headers);
	yield return download;
	
	if(!string.IsNullOrEmpty(download.error))
		Debug.Log("Newgrounds Error: "+download.error);
		
	Debug.Log("Newgrounds: "+download.text);
		
	parseJSON (download.text);
}

    IEnumerator loadMySite (){
	WWW download = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=loadMySite&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&host=" + URL), headers);
	yield return download;
	parseJSON (download.text);
}

    IEnumerator saveFile ( string saveGroup ,   string fileName ,   string description ,   string file ,   Texture2D thumbnail  ){
	int saveGroupInt = -2;
	for (int i = 0; i<SaveGroups.Length; i++) {
		if (SaveGroups[i].name == saveGroup)
			saveGroupInt = SaveGroups[i].id;
}	var seed= genSeed ();
    var JSON= "{\"session_id\":\"" + SessionID + "\",\"user_name\":\"" + UserName + "\",\"description\":\"" + description + "\",\"user_email\":null,\"seed\":\"" + seed + "\",\"publisher_id\":" + PublisherId /*+ ',"keys":' + keys */ + ",\"ratings\":[],\"filename\":\"" + fileName + "\",\"group\":" + saveGroupInt + ",\"command_id\":\"saveFile\"}";
	//guiText.text = JSON + "\n";
    JSON = encrypt (seed, JSON);
	byte[] bytes = thumbnail.EncodeToPNG();
    
    var form= new WWWForm();
    
    form.AddField ("command_id", "securePacket");
    form.AddField ("secure", JSON);
    form.AddField ("tracker_id", APIID);
    form.AddField ("Filename", "thumbnail");
    form.AddBinaryData ("thumbnail", bytes, "thumbnail", "image/png");
    form.AddField ("Filename", "file");
    form.AddBinaryData ("file", encoding.GetBytes(file), "file", "text/plain");
    form.AddField ("Upload", "Submit Query");
    
	WWW download = new WWW ("http://www.ngads.com/gateway_v2.php",form);
	yield return download;
	parseJSON (download.text);
}

IEnumerator SecurePacket ( string seed ,   string text  ){
	Debug.Log("Newgrounds send: "+text);
	text = encrypt (seed, text);
	text = "command%5Fid=securePacket&secure=" + WWW.EscapeURL(text) + "&tracker%5Fid=" + WWW.EscapeURL(APIID);
	Debug.Log("Newgrounds send-encrypt: "+text);
	WWW download = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes(text), headers);
	yield return download;
	
	if(!string.IsNullOrEmpty(download.error)) {
		Debug.Log("Newgrounds Error: "+download.error);
            errorMessage = download.error;
		success = false;
	}
	else {
		//guiText.text += ("\n" + download.text);
		Debug.Log("Newgrounds text: "+download.text);
		parseJSON (download.text);
	}
}

string encrypt ( string seed ,   string text  ){
	string hash = Md5Sum (seed);
	text = EnDeCrypt(text);
	text = StrToHexStr(text);
	text = hash + text;
	text = radix(text);
	return text;
}

string decrypt ( string g  ){
	g = g.Substring (1);
	g = deradix (g);
	g = g.Substring (32);
	g = HexStrToStr (g);
	g = EnDeCrypt (g);
	return g;
}

    string genSeed (){
	string seed = "";
	int iter = Random.Range (8,16);
	for (int i = 0; i < iter; i++) {
		var a = Random.Range (-26,36);
		if (a == -26)
			seed += "z";
		else if (a == -25)
			seed += "y";
		else if (a == -24)
			seed += "x";
		else if (a == -23)
			seed += "w";
		else if (a == -22)
			seed += "v";
		else if (a == -21)
			seed += "u";
		else if (a == -20)
			seed += "t";
		else if (a == -19)
			seed += "s";
		else if (a == -18)
			seed += "r";
		else if (a == -17)
			seed += "q";
		else if (a == -16)
			seed += "p";
		else if (a == -15)
			seed += "o";
		else if (a == -14)
			seed += "n";
		else if (a == -13)
			seed += "m";
		else if (a == -12)
			seed += "l";
		else if (a == -11)
			seed += "k";
		else if (a == -10)
			seed += "j";
		else if (a == -9)
			seed += "i";
		else if (a == -8)
			seed += "h";
		else if (a == -7)
			seed += "g";
		else if (a == -6)
			seed += "f";
		else if (a == -5)
			seed += "e";
		else if (a == -4)
			seed += "d";
		else if (a == -3)
			seed += "c";
		else if (a == -2)
			seed += "b";
		else if (a == -1)
			seed += "a";
		else if (a == 26)
			seed += "Z";
		else if (a == 25)
			seed += "Y";
		else if (a == 24)
			seed += "X";
		else if (a == 23)
			seed += "W";
		else if (a == 22)
			seed += "V";
		else if (a == 21)
			seed += "U";
		else if (a == 20)
			seed += "T";
		else if (a == 19)
			seed += "S";
		else if (a == 18)
			seed += "R";
		else if (a == 17)
			seed += "Q";
		else if (a == 16)
			seed += "P";
		else if (a == 15)
			seed += "O";
		else if (a == 14)
			seed += "N";
		else if (a == 13)
			seed += "M";
		else if (a == 12)
			seed += "L";
		else if (a == 11)
			seed += "K";
		else if (a == 10)
			seed += "J";
		else if (a == 9)
			seed += "I";
		else if (a == 8)
			seed += "H";
		else if (a == 7)
			seed += "G";
		else if (a == 6)
			seed += "F";
		else if (a == 5)
			seed += "E";
		else if (a == 4)
			seed += "D";
		else if (a == 3)
			seed += "C";
		else if (a == 2)
			seed += "B";
		else if (a == 1)
			seed += "A";
		else if (a == 0)
			seed += "0";
		else
			seed += (a-26);
}	return seed;
}

string Md5Sum ( string strToEncrypt  ){
	var bytes= encoding.GetBytes(strToEncrypt);
	var md5= new System.Security.Cryptography.MD5CryptoServiceProvider();
	byte[] hashBytes = md5.ComputeHash(bytes);
	string hashString = "";
	for (int i = 0; i < hashBytes.Length; i++) {
		hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, "0"[0]);
}	return hashString.PadLeft(32, '0');
}

string EnDeCrypt ( string text  ){
	RC4Initialize();
	int i = 0;
	int j = 0;
	int k = 0;
		string cipher = "";
		for (int a = 0; a < text.Length; a++) {
			i = ((i + 1) % 256);
			j = ((j + sbox[i]) % 256);
			int tempSwap = sbox[i];
			sbox[i] = sbox[j];
			sbox[j] = tempSwap;
			k = sbox[(sbox[i] + sbox[j]) % 256];
			int cipherBy = text[a];
			cipherBy = cipherBy ^ k;
			cipher += (System.Convert.ToChar(cipherBy));
}		return cipher;
}

string StrToHexStr ( string str  ){
	string sb = "";
	for (int i = 0; i < str.Length; i++) {
		int v = str[i];
		sb += (string.Format("{0:X2}", v));
}	return sb;
}

string HexStrToStr ( string hexStr  ){
	string sb = "";
	for (int i = 0; i < hexStr.Length; i += 2) {
		int n = System.Int32.Parse(hexStr.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
			sb += (System.Convert.ToChar(n)); 
}	return sb;
}
	
void RC4Initialize (){
	sbox = new int[256];
	key = new int[256];
	n = password.Length;
	for (int a = 0; a < 256; a++)	{
		key[a] = (password[a % n]);
		sbox[a] = a;
}	int b = 0;
		for (int a = 0; a < 256; a++)	{
			b = (b + sbox[a] + key[a]) % 256;
			int tempSwap = sbox[a];
			sbox[a] = sbox[b];
			sbox[b] = tempSwap;
}}

string radix ( string g  ){
	string output = "";
	for (int i = 0; (i< g.Length-6); i+=6) {
		string block = "";
		int d = System.Int32.Parse(g.Substring(i,6), System.Globalization.NumberStyles.AllowHexSpecifier);
		for (int j = 0; j<4; j++){
			int l = d % 79;
			char car = rad[l];
			d -= l;
			d /= 79;
			block = car + block;
}		output +=block;
}	string block0 = "";
	string p = g.Substring(g.Length-(g.Length%6),(g.Length%6));
	if (p != "") {
		int o = System.Int32.Parse(g.Substring(g.Length-(g.Length%6),(g.Length%6)), System.Globalization.NumberStyles.AllowHexSpecifier);
		for (int j = 0; j<4; j++) {
			int l0 = o % 79;
			char car0 = rad[l0];
			o -= l0;
			o /= 79;
			block0 = car0 + block0;
}		output +=block0;
}	output = ((g.Length) % 6) + output;
	return output;
}

string deradix ( string g  ){
	string output = "";
	for (int i= 0; i < g.Length-3; i += 4) {
		string opal = g.Substring (i,4);
		int ruby = 0;
		for (int j = 0; j < rad.Length; j++) {
			if (opal[0] == rad[j])
				ruby += (j*(493039));
			if (opal[1] == rad[j])
				ruby += (j*(6241));
			if (opal[2] == rad[j])
				ruby += (j*(79));
			if (opal[3] == rad[j])
				ruby += j;
}		string onix = "";
		for (int k = 0; k < 6; k++) {
			int lapis = (ruby % 16);
			onix = HexRad[lapis] + onix;
			ruby -= lapis;
			ruby /= 16;
}	output += onix;
}	return output;
}

//bob ("http://uploads.ungrounded.net/tmp/744000/744865/file/alternate/alternate_2.zip/?NewgroundsAPI_PublisherID=1&NewgroundsAPI_SandboxID=52fe167ea1067&NewgroundsAPI_SessionID=ZC6Q2REMWCAMeVeskZs1813ce389f27095b178f1ec742f35d0ccba83d9f6Tm7f&NewgroundsAPI_UserName=Bcadren&NewgroundsAPI_UserID=4889253&ng_username=Bcadren");
//copy of a valid URL for offline testing

void bob ( string data  ){
	string[] Delimiters = new string[] { "?", "&", "=" };
	string[] p = data.Split (Delimiters, System.StringSplitOptions.RemoveEmptyEntries);
	for (int i = 0; i < p.Length; i++)
		p[i] = WWW.UnEscapeURL (p[i]);
	for (int j = 0; j < p.Length; j++){
		if (p[j] == "NewgroundsAPI_SessionID")
			SessionID = p[j+1];
		else if (p[j] == "NewgroundsAPI_UserID")
			UserID = int.Parse(p[j+1]);
		else if (p[j] == "NewgroundsAPI_UserName")
			UserName = p[j+1];
		else if (p[j] == "NewgroundsAPI_PublisherID")
			PublisherId = int.Parse(p[j+1]);
}	URL = p[0];
	for (int k = 0; k < URL.Length; k++)
		if ((URL[k] == '/') && (URL[k+1] != '/') && (URL[k-1] != '/'))
			URL = URL.Substring (0, k);
	if (UserName == "&lt;deleted&gt;")
		UserName = DefaultUsername;
}
}