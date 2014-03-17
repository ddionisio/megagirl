private var headers : Hashtable = new Hashtable();
private var encoding : System.Text.Encoding = new System.Text.UTF8Encoding();
private var rad : String = "/g8236klvBQ#&|;Zb*7CEA59%s`Oue1wziFp$rDVY@TKxUPWytSaGHJ>dmoMR^<0~4qNLhc(I+fjn)X";
private var HexRad : String = "0123456789ABCDEF";
private var sbox : int[];
private var key : int[];
private var n : int;
private var URL : String;
var APIID : String;
var password : String;
var preloadSettings : boolean;
var versionNumber : String;
var DefaultUsername : String;
//var flashPreloader : boolean;
var SessionID : String;
var UserID : int;
var UserName : String;
var PublisherId : int;
var Medals : medal[];
var SaveGroups : SaveGroup[];
var boardNames : String[];
var boardIndices : int[];
var errorMessage : String;
var errorCode : int;
var success : boolean;

private var mStarted : boolean;
private var mWorking : boolean;

public class medal {
	public var id : int;
	public var name : String;
	public var points : int;
	public var difficulty : int;
	public var unlocked : boolean;
	public var secret : boolean;
	public var description : String;
	public var url : String;
	public var icon : Texture2D;

	public function medal (i : int, n : String, p : int, d : int, u : boolean, s : boolean, de : String, ur : String) {
		id = i;
		name = n;
		points = p;
		difficulty = d;
		unlocked = u;
		secret = s;
		description = de;
		url = ur;
}}

public class rating {
	public var id : int;
	public var name : String;
	public var min : int;
	public var max : int;
	public var isFloat : boolean;
	
	public function rating (i : int, n : String, mi : int, ma : int, fl : boolean) {
		id = i;
		name = n;
		min = mi;
		max = ma;
		isFloat = fl;
}}

public class key {
	public var id : int;
	public var name : String;
	public var type : String;
	
	public function key (i : int, n : String, t : int) {
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
	public var id : int;
	public var name : String;
	public var type : String;
	public var keys : key[];
	public var ratings : rating[];
	
	public function SaveGroup (i : int, n : String, t : int, k : key[], r : rating[]) {
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

Application.ExternalEval(" UnityObject2.instances[0].getUnity().SendMessage('" + name + "','bob', document.location.href);");

function HasStarted() {
	return mStarted;
}

function IsWorking() {
	return mWorking;
}

function Start () {
	mWorking = true;
	
	var a : String = '[{"id": 316, "value":"23.456"}, {"id":315, "value":"Google"}, {"id":318, "value":"true"}, {"id":317,"value":317}]';
	headers.Add("Content-Type", "application/x-www-form-urlencoded");
	headers.Add("Accept","*/*");
	DontDestroyOnLoad (transform.gameObject);
	yield WaitForSeconds (5);
	yield registerSession();
//	if (flashPreloader) {
//		Application.ExternalEval ("hideUnity()");

	mStarted = true;
	mWorking = false;
	
	Debug.Log("Newgrounds Started SessionID: "+SessionID+" UserID: "+UserID+" Name: "+UserName);
}

function IsLoggedIn() {
	return SessionID.Length > 0;
}

function downloadMedal (s : String) {
	var m : int = findMedal (s);
	var download : WWW = new WWW (Medals[m].url);
	Medals[m].icon = new Texture2D (1,1);
	yield download;
	download.LoadImageIntoTexture(Medals[m].icon);
}

function downloadAllMedals () {
	for (i=0; i<Medals.Length; i++) {
		var download : WWW = new WWW (Medals[i].url);
		Medals[i].icon = new Texture2D (1,1);
		yield download;
		download.LoadImageIntoTexture(Medals[i].icon);
}}

function findMedal (s : String) {
	Debug.Log("Newgrounds Find Medal: "+s);
	for (i = 0; i < Medals.Length; i++) {
		if (Medals[i].name == s)
			return i;
}	throw UnityException ("Medal Doesn't Exist");
}

function unlockMedal (s : String) {
	Debug.Log("Newgrounds Unlock Medal: "+s);

	mWorking = true;

	var a : int = findMedal(s);
	Medals[a].unlocked = true;
	a = Medals[a].id;
	var seed : String = genSeed ();
	var text : String = '{"command_id":"unlockMedal","publisher_id":' + PublisherId + ',"session_id":"' + SessionID + '","medal_id":' + a + ',"seed":"' + seed + '"}';
	yield SecurePacket (seed, text);
			
	mWorking = false;
	Debug.Log("Newgrounds Finish Unlocking Medal: "+s);
}

function postScore (score : int, BoardName : String) {
	Debug.Log("Newgrounds submit score to "+BoardName);
	
	mWorking = true;
	
	var BoardId : int;
	for (i = 0; i < boardNames.Length; i++){
		if (BoardName == boardNames[i])
			BoardId = boardIndices[i];
}	var seed : String = genSeed ();
	var text : String = '{"command_id":"postScore","publisher_id":' + PublisherId + ',"session_id":"' + SessionID + '","board":' + BoardId + ',"value":' + score + ',"seed":"' + seed + '"}';
	yield SecurePacket (seed, text);
	
	mWorking = false;
	Debug.Log("Newgrounds Finish Submitting Score: "+BoardName);
}

function getMedals () {
	mWorking = true;
	
	var download : WWW = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=getMedals&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&publisher%5Fid=" + PublisherId + "&user%5Fid=" + UserID), headers);
	yield download;
	
	//Debug.Log(download.text);
			
	mWorking = false;
	Debug.Log("Newgrounds Finish Getting Medals");
	if(!String.IsNullOrEmpty(download.error)) {
		Debug.Log("Newgrounds Error: "+download.error);
		success = false;
	}
	else {
		parseJSON (download.text);
	}
	
	if(Medals.Length > 0) {		
		for(i = 0; i < Medals.Length; i++) {
			Debug.Log("Medal: "+Medals[i].id+" name: "+Medals[i].name);
		}
	}
	else
		Debug.Log("Empty Medals WTF");
}

function parseJSON (str : String) {
	var left : boolean = true;
	var commandId : String;
	var itemName : String;
	var item : String;
	var isNum : boolean = false;
	var start : int;
	var isString : boolean = false;
	
	var ID : int = 0;
	var name0 : String;
	var type : int;
	var min : int;
	var max : int;
	var isFloat : boolean;
	
	var tempKeys : key[] = [];
	var tempRatings : rating[] = [];
	
	var medalID : int = 0;
	var medalName : String;
	var medalValue : int;
	var medalDifficulty : int;
	var medalUnlocked : boolean;
	var secret : boolean;
	var medalIcon : String;
	var medalDescription : String;
	
	var duplicate : boolean = false;
	
	var scoreBoards : boolean = false;
	var saveGroups0 : boolean = false;
	var keys : boolean = false;
	var ratings : boolean = false;
	
	var groupID : int;
	var groupName : String;
	var groupType : int;
	
	errorCode = -1;
	errorMessage = "";
	success = true;
	
	if ((str[0] != "{") && (str[1] != "{")) {
		throw new UnityException ("Command Failed: Unknown Command");
}	for (i = 0; i < str.Length; i++) {
		if (isString) {
			if ((str[i] == '"') && (str[i-1] != "\\")){
				if (left) {
					itemName = str.Substring (start, (i-start));
					if (itemName == "score_boards")
						scoreBoards = true;
					if (itemName == "save_groups")
						saveGroups0 = true;
					if (itemName == "keys") {
						keys = true;
						tempKeys = [];
}					if (itemName == "ratings") {
						ratings = true;
						tempRatings = [];
}}				else {
					item = str.Substring (start, (i-start));
					if (itemName == "command_id")
						commandId = item;
					if (itemName == "medal_name")
						medalName = item;
					if (itemName == "medal_icon"){
						for (z = 0; z < item.Length; z++){
							if (item[z] == "\\")
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
}}		else if (str[i] == ":"){
			left = false;
			if (str[i+1] != '"') {
				start = (i+1);
				isNum = true;
}}		else if (str[i] == '"') {
			isString = true;
			start = (i+1);
}		
if(!isString) {
if ((str[i] == ",") || (str[i] == "}")) {
			left = true;
			if (isNum)  {
				item = str.Substring (start, (i-start));
				if (itemName == "medal_id")
					medalID = parseInt (item);
				if (itemName == "medal_value" )
					medalValue = parseInt (item);
				if (itemName == "medal_difficulty")
					medalDifficulty = parseInt (item);
				if (itemName == "id")
					ID = parseInt (item);
				if (itemName == "type")
					type = parseInt (item);
				if (itemName == "min")
					min = parseInt (item);
				if (itemName == "max")
					max = parseInt (item);
				if (itemName == "group_id")
					groupID = parseInt (item);
				if (itemName == "group_type")
					groupType = parseInt (item);
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
					errorCode = parseInt(item);
}				if ((itemName == "success") && (item == "0"))
					success = false;
					//throw new UnityException ("Command Failed: " + commandId);				
				isNum = false;
}}		if (str[i] == "}") {
			if (medalID != 0){
				for (j = 0; j < Medals.Length; j++) {
					if (medalID == Medals[j].id)
						duplicate = true;
}				if (!duplicate) {
					var temp : medal[] = new medal[Medals.Length + 1];
					temp[temp.Length - 1] = medal(medalID, medalName, medalValue, medalDifficulty, medalUnlocked, secret, medalDescription, medalIcon);
					for(var k : int = 0; k < Medals.Length; k++) {
						temp[k] = Medals[k];
}					Medals = temp;
				medalID = 0;
}}			else if (scoreBoards) {
				for (l = 0; l < boardIndices.Length; l++) {
					if (ID == boardIndices[l])
						duplicate = true;
}				if (!duplicate) {
					var temp0 : String[] = new String[boardNames.Length + 1];
					var temp1 : int[] = new int[boardIndices.Length + 1];
					temp0[temp0.Length - 1] = name0;
					temp1[temp1.Length - 1] = ID;
					for(var m : int = 0; m < boardNames.Length; m++) {
						temp0[m] = boardNames[m];
						temp1[m] = boardIndices[m];
}					boardNames = temp0;
					boardIndices = temp1;
}}			else if (keys) {
				var temp2 : key[] = new key[tempKeys.Length + 1];
				temp2[temp2.Length-1] = key(ID, name0, type);
				for (var o : int = 0; o < tempKeys.Length; o++) 
					temp2[o] = tempKeys[o];
				tempKeys = temp2;
}			else if (ratings) {
				var temp3 : rating[] = new rating[tempRatings.Length + 1];
				temp3[temp3.Length-1] = rating(ID, name0, min, max, isFloat);
				for (var p : int = 0; p < tempRatings.Length; p++) 
					temp3[p] = tempRatings[p];
				tempRatings = temp3;
}			else if (saveGroups0){
				for (q = 0; q < SaveGroups.Length; q++) {
					if (groupID == SaveGroups[q])
						duplicate = true;
}				if (!duplicate) {
					var temp4 : SaveGroup[] = new SaveGroup[SaveGroups.Length + 1];
					temp4[temp4.Length - 1] = SaveGroup(groupID, groupName, groupType, tempKeys, tempRatings);
					for(var r : int = 0; r < SaveGroups.Length; r++) {
						temp4[r] = SaveGroups[r];
}					SaveGroups = temp4;
}}			duplicate = false;
}		else if (str[i] == "{") 
			left = true;
		else if (str[i] == "]") {
			scoreBoards = false;
			if (!keys && !ratings)
				saveGroups0 = false;
			keys = false;
			ratings = false;
}}}

	if(!success && errorMessage.Length == 0)
		errorMessage = commandId;

}

function loadSettings () {
	var download : WWW = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=preloadSettings&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&publisher%5Fid=" + PublisherId + "&user%5Fid=" + UserID), headers);
	yield download;
	
	mWorking = false;
	Debug.Log("Newgrounds Finish Getting Settings");
	if(!String.IsNullOrEmpty(download.error)) {
		Debug.Log("Newgrounds Error: "+download.error);
		success = false;
	}
	else {
		Debug.Log("Newgrounds Settings: "+download.text);
		parseJSON (download.text);
	}
	
	if(Medals.Length > 0) {		
		for(i = 0; i < Medals.Length; i++) {
			Debug.Log("Medal: "+Medals[i].id+" name: "+Medals[i].name);
		}
	}
	
	parseJSON (download.text);
}

function registerSession () {
	var a : String = "";
	if (preloadSettings)
		a = "&preload=1";
	var download : WWW = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=connectMovie&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&publisher%5Fid=" + PublisherId + "&user%5Fid=" + UserID + "&host=" + URL + a +"&movie_version=" + versionNumber), headers);
	yield download;
	
	if(!String.IsNullOrEmpty(download.error))
		Debug.Log("Newgrounds Error: "+download.error);
		
	Debug.Log("Newgrounds: "+download.text);
		
	parseJSON (download.text);
}

function loadMySite () {
	var download : WWW = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes("command%5Fid=loadMySite&tracker%5Fid=" + WWW.EscapeURL(APIID) + "&host=" + URL), headers);
	yield download;
	parseJSON (download.text);
}

function saveFile (saveGroup: String, fileName: String, description: String, file: String, thumbnail: Texture2D) {
	var saveGroupInt : int = -2;
	for (i = 0; i<SaveGroups.Length; i++) {
		if (SaveGroups[i].name == saveGroup)
			saveGroupInt = SaveGroups[i].id;
}	var seed = genSeed ();
    var JSON = '{"session_id":"' + SessionID + '","user_name":"' + UserName + '","description":"' + description + '","user_email":null,"seed":"' + seed + '","publisher_id":' + PublisherId /*+ ',"keys":' + keys */ + ',"ratings":[],"filename":"' + fileName + '","group":' + saveGroupInt + ',"command_id":"saveFile"}';
	//guiText.text = JSON + "\n";
    JSON = encrypt (seed, JSON);
	var bytes : byte[] = thumbnail.EncodeToPNG();
    
    var form = new WWWForm();
    
    form.AddField ("command_id", "securePacket");
    form.AddField ("secure", JSON);
    form.AddField ("tracker_id", APIID);
    form.AddField ("Filename", "thumbnail");
    form.AddBinaryData ("thumbnail", bytes, "thumbnail", "image/png");
    form.AddField ("Filename", "file");
    form.AddBinaryData ("file", encoding.GetBytes(file), "file", "text/plain");
    form.AddField ("Upload", "Submit Query");
    
	var download : WWW = new WWW ("http://www.ngads.com/gateway_v2.php",form);
	yield download;
	parseJSON (download.text);
}

function SecurePacket (seed : String, text : String) {
	Debug.Log("Newgrounds send: "+text);
	text = encrypt (seed, text);
	text = "command%5Fid=securePacket&secure=" + WWW.EscapeURL(text) + "&tracker%5Fid=" + WWW.EscapeURL(APIID);
	Debug.Log("Newgrounds send-encrypt: "+text);
	var download : WWW = new WWW ("http://www.ngads.com/gateway_v2.php", encoding.GetBytes(text), headers);
	yield download;
	
	if(!String.IsNullOrEmpty(download.error)) {
		Debug.Log("Newgrounds Error: "+download.error);
		error = download.error;
		success = false;
	}
	else {
		//guiText.text += ("\n" + download.text);
		Debug.Log("Newgrounds text: "+download.text);
		parseJSON (download.text);
	}
}

function encrypt (seed : String, text : String) {
	var hash : String = Md5Sum (seed);
	text = EnDeCrypt(text);
	text = StrToHexStr(text);
	text = hash + text;
	text = radix(text);
	return text;
}

function decrypt (g : String) {
	g = g.Substring (1);
	g = deradix (g);
	g = g.Substring (32);
	g = HexStrToStr (g);
	g = EnDeCrypt (g);
	return g;
}

function genSeed () {
	var seed : String = "";
	var iter : int = Random.Range (8,16);
	for (i = 0; i < iter; i++) {
		a = Random.Range (-26,36);
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

function Md5Sum (strToEncrypt: String) {
	var bytes = encoding.GetBytes(strToEncrypt);
	var md5 = System.Security.Cryptography.MD5CryptoServiceProvider();
	var hashBytes:byte[] = md5.ComputeHash(bytes);
	var hashString = "";
	for (var i = 0; i < hashBytes.Length; i++) {
		hashString += System.Convert.ToString(hashBytes[i], 16).PadLeft(2, "0"[0]);
}	return hashString.PadLeft(32, "0"[0]);
}

function EnDeCrypt(text : String) {
	RC4Initialize();
	var i : int = 0;
	var j : int = 0;
	var k : int = 0;
		var cipher = "";
		for (a = 0; a < text.Length; a++) {
			i = ((i + 1) % 256);
			j = ((j + sbox[i]) % 256);
			var tempSwap : int = sbox[i];
			sbox[i] = sbox[j];
			sbox[j] = tempSwap;
			k = sbox[(sbox[i] + sbox[j]) % 256];
			var cipherBy : int = text[a];
			cipherBy = cipherBy ^ k;
			cipher += (System.Convert.ToChar(cipherBy));
}		return cipher;
}

function StrToHexStr(str : String) {
	var sb = "";
	for (i = 0; i < str.Length; i++) {
		var v : int = str[i];
		sb += (String.Format("{0:X2}", v));
}	return sb;
}

function HexStrToStr(hexStr : String) {
	var sb = "";
	for (i = 0; i < hexStr.Length; i += 2) {
		var n : int = System.Int32.Parse(hexStr.Substring(i, 2), System.Globalization.NumberStyles.AllowHexSpecifier);
			sb += (System.Convert.ToChar(n)); 
}	return sb;
}
	
function RC4Initialize() {
	sbox = new int[256];
	key = new int[256];
	n = password.Length;
	for (a = 0; a < 256; a++)	{
		key[a] = (password[a % n]);
		sbox[a] = a;
}	var b : int = 0;
		for (a = 0; a < 256; a++)	{
			b = (b + sbox[a] + key[a]) % 256;
			var tempSwap : int = sbox[a];
			sbox[a] = sbox[b];
			sbox[b] = tempSwap;
}}

function radix (g : String) {
	var output : String = "";
	for (i = 0; (i< g.Length-6); i+=6) {
		var block : String = "";
		var d : int = System.Int32.Parse(g.Substring(i,6), System.Globalization.NumberStyles.AllowHexSpecifier);
		for (j = 0; j<4; j++){
			var l : int = d % 79;
			var car : char = rad[l];
			d -= l;
			d /= 79;
			block = car + block;
}		output +=block;
}	var block0 : String = "";
	var p : String = g.Substring(g.Length-(g.Length%6),(g.Length%6));
	if (p != "") {
		var o : int = System.Int32.Parse(g.Substring(g.Length-(g.Length%6),(g.Length%6)), System.Globalization.NumberStyles.AllowHexSpecifier);
		for (j = 0; j<4; j++) {
			var l0 : int = o % 79;
			var car0 : char = rad[l0];
			o -= l0;
			o /= 79;
			block0 = car0 + block0;
}		output +=block0;
}	output = ((g.Length) % 6) + output;
	return output;
}

function deradix (g : String) {
	var output : String = "";
	for (i= 0; i < g.Length-3; i += 4) {
		var opal : String = g.Substring (i,4);
		var ruby : int = 0;
		for (j = 0; j < rad.Length; j++) {
			if (opal[0] == rad[j])
				ruby += (j*(493039));
			if (opal[1] == rad[j])
				ruby += (j*(6241));
			if (opal[2] == rad[j])
				ruby += (j*(79));
			if (opal[3] == rad[j])
				ruby += j;
}		var onix : String = "";
		for (k = 0; k < 6; k++) {
			var lapis : int = (ruby % 16);
			onix = HexRad[lapis] + onix;
			ruby -= lapis;
			ruby /= 16;
}	output += onix;
}	return output;
}

//bob ("http://uploads.ungrounded.net/tmp/744000/744865/file/alternate/alternate_2.zip/?NewgroundsAPI_PublisherID=1&NewgroundsAPI_SandboxID=52fe167ea1067&NewgroundsAPI_SessionID=ZC6Q2REMWCAMeVeskZs1813ce389f27095b178f1ec742f35d0ccba83d9f6Tm7f&NewgroundsAPI_UserName=Bcadren&NewgroundsAPI_UserID=4889253&ng_username=Bcadren");
//copy of a valid URL for offline testing

function bob (data : String) {
	var Delimiters : String[] = ['?','&','='];
	var p : String[] = data.Split (Delimiters, System.StringSplitOptions.RemoveEmptyEntries);
	for (i = 0; i < p.length; i++)
		p[i] = WWW.UnEscapeURL (p[i]);
	for (j = 0; j < p.length; j++){
		if (p[j] == "NewgroundsAPI_SessionID")
			SessionID = p[j+1];
		else if (p[j] == "NewgroundsAPI_UserID")
			UserID = parseInt(p[j+1]);
		else if (p[j] == "NewgroundsAPI_UserName")
			UserName = p[j+1];
		else if (p[j] == "NewgroundsAPI_PublisherID")
			PublisherId = parseInt(p[j+1]);
}	URL = p[0];
	for (k = 0; k < URL.Length; k++)
		if ((URL[k] == "/") && (URL[k+1] != "/") && (URL[k-1] != "/"))
			URL = URL.Substring (0, k);
	if (UserName == "&lt;deleted&gt;")
		UserName = DefaultUsername;
}