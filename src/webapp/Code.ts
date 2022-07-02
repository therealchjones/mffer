/**
 * Dummy function, present to simplify getting permissions in the Google Scripts
 * IDE. This should be the first method in the first file.
 * @returns {boolean} true
 */
function getPermissions(): boolean {
	Logger.log("Permissions granted.");
	return true;
}
/**
 * Remove all properties from the PropertiesService stores associated with this
 * script, effectively resetting the deployment. It also erases the entire
 * associated spreadsheet. Private function specified with the trailing _ cannot
 * be run from the client side or easily selected in the Apps Script IDE. To run
 * (and remove all this data), open in the Apps Script IDE, uncomment the below
 * line starting with "function", press "Save", select
 * "RESET_DEPLOYMENT_YES_REALLY" from the function drop-down, and press "Run".
 */
// function RESET_DEPLOYMENT_YES_REALLY() { resetAllProperties_(); }
function resetAllProperties_(): void {
	let properties = PropertiesService.getDocumentProperties();
	if (properties != null) properties.deleteAllProperties();
	properties = PropertiesService.getUserProperties();
	if (properties != null) properties.deleteAllProperties();
	properties = PropertiesService.getScriptProperties();
	if (properties != null) properties.deleteAllProperties();
	eraseSpreadsheet_();
	removeAppData_();
}
/**
 * Get the properties store. Though likely not necessary to separate
 * user/script/document stores, having a single function will allow
 * standardization. We choose 'user' as it is the most restrictive.
 * @returns {GoogleAppsScript.Properties.Properties} user properties
 */
function getProperties_(): GoogleAppsScript.Properties.Properties {
	return PropertiesService.getUserProperties();
}
function setProperties_(
	properties: { [index: string]: string } | null = null
): void {
	if (properties == null) properties = {};
	if (!isFalseOrEmpty_(properties.oauthId))
		setProperty_("oauthId", properties.oauthId);
	if (!isFalseOrEmpty_(properties.oauthSecret))
		setProperty_("oauthSecret", properties.oauthSecret);
	if (!isFalseOrEmpty_(properties.pickerApiKey))
		setProperty_("pickerApiKey", properties.pickerApiKey);
	if (!isFalseOrEmpty_(properties.adminId))
		setProperty_("adminId", properties.adminId);
	if (!isFalseOrEmpty_(properties.hostUri))
		setProperty_("hostUri", properties.hostUri);
}
/**
 * The basic webapp-enabling function responding to the HTTP GET request
 * @returns {GoogleAppsScript.HTML.HtmlOutput} web page appropriate to the
 * request
 */
function doGet(): GoogleAppsScript.HTML.HtmlOutput {
	return buildPage_();
}
/**
 * Construct the web page from the Index.html template
 * @returns Apps Script-compatible web page
 */
function buildPage_(
	storage: VolatileProperties = null
): GoogleAppsScript.HTML.HtmlOutput {
	let properties = getProperties_();
	let contents = include("Index.html", storage);
	let page = HtmlService.createHtmlOutput(contents)
		.addMetaTag(
			"viewport",
			"width=device-width, initial-scale=1, shrink-to-fit=no"
		)
		.setTitle("mffer: Marvel Future Fight exploration & reporting");
	page.setXFrameOptionsMode(HtmlService.XFrameOptionsMode.ALLOWALL);
	return page;
}
function getConfig() {
	let config = {
		oauthId: getOauthId_(),
		oauthSecret: hasOauthSecret_(),
		pickerApiKey: getPickerApiKey_(),
	};
	return JSON.stringify(config);
}
function isConfigured(): boolean {
	return (
		!isFalseOrEmpty_(getOauthId_()) &&
		!isFalseOrEmpty_(getOauthSecret_()) &&
		!isFalseOrEmpty_(getAdminId_()) &&
		!isFalseOrEmpty_(getUsersFileId_())
	);
}
function getProperty_(propertyName: string): string | null {
	var properties = getProperties_();
	if (properties === null) {
		return null;
	}
	return properties.getProperty(propertyName);
}
/**
 * Get the Google Sheet containing mffer data
 * @returns {GoogleAppsScript.Spreadsheet.Spreadsheet} The sheet (workbook)
 * containing mffer data, or null if none exists
 */
function getSpreadsheet_(): GoogleAppsScript.Spreadsheet.Spreadsheet {
	return SpreadsheetApp.getActiveSpreadsheet();
}
function getOauthId_(): string | null {
	return getProperty_("oauthId");
}
function getOauthSecret_(): string | null {
	return getProperty_("oauthSecret");
}
function getPickerApiKey_(): string | null {
	return getProperty_("pickerApiKey");
}
function hasOauthSecret_(): boolean {
	return getOauthSecret_() != null;
}
function getAdminId_(): string | null {
	return getProperty_("adminId");
}
function getHostUri_(): string | null {
	return getProperty_("hostUri");
}
function setProperty_(propertyName: string, propertyValue: string) {
	var properties = getProperties_();
	if (properties == null) {
		throw "Unable to access script properties";
	}
	properties.setProperty(propertyName, propertyValue);
}
function getAdminAuthService_(storage: VolatileProperties = null) {
	let callbackFunction: string,
		oauthId: string | null,
		oauthSecret: string | null = null;
	if (storage == null) storage = new VolatileProperties();
	oauthId = storage.getProperty("oauthId");
	oauthSecret = storage.getProperty("oauthSecret");
	if (isConfigured()) {
		callbackFunction = "processAdminAuthResponse_";
		if (isFalseOrEmpty_(oauthId)) oauthId = getOauthId_();
		if (isFalseOrEmpty_(oauthSecret)) oauthSecret = getOauthSecret_();
	} else {
		callbackFunction = "processNewAdminAuthResponse_";
	}
	if (isFalseOrEmpty_(oauthId) || isFalseOrEmpty_(oauthSecret)) {
		throw new Error(
			"Unable to create admin service: OAuth ID and OAuth secret are required."
		);
	}
	if (oauthId == null || oauthSecret == null) throw new Error();
	return OAuth2.createService("adminLogin")
		.setAuthorizationBaseUrl("https://accounts.google.com/o/oauth2/auth")
		.setTokenUrl("https://accounts.google.com/o/oauth2/token")
		.setClientId(oauthId)
		.setClientSecret(oauthSecret)
		.setCallbackFunction(callbackFunction)
		.setPropertyStore(storage)
		.setScope(
			"openid https://www.googleapis.com/auth/drive.appdata https://www.googleapis.com/auth/drive.file"
		)
		.setParam("access_type", "offline")
		.setParam("prompt", "consent");
}
function isFalseOrEmpty_(check: string | boolean | null): boolean {
	if (
		!check ||
		check.toString().trim() === "" ||
		JSON.stringify(check) == "{}"
	)
		return true;
	return false;
}
function getAdminAuthUrl(propertiesJson: string | null = null) {
	let properties: { [index: string]: string };
	if (propertiesJson == null) properties = {};
	else properties = JSON.parse(propertiesJson);
	if (properties == null) properties = {};
	if (!isConfigured()) {
		if (
			isFalseOrEmpty_(properties.oauthId) ||
			isFalseOrEmpty_(properties.oauthSecret)
		)
			throw new Error("OAuth ID and OAuth secret are not set.");
		// discard all other settings; new config will set only these
		let loginProperties = {
			oauthId: properties.oauthId,
			oauthSecret: properties.oauthSecret,
		};
		let storage = new VolatileProperties(loginProperties);
		return getAdminAuthService_(storage).getAuthorizationUrl(
			loginProperties
		);
	}
	let newProperties: { [index: string]: string } = {};
	if (!isFalseOrEmpty_(properties.oauthId))
		newProperties.oauthId = properties.oauthId;
	if (!isFalseOrEmpty_(properties.oauthSecret))
		newProperties.oauthSecret = properties.oauthSecret;
	if (!isFalseOrEmpty_(properties.pickerApiKey))
		newProperties.pickerApiKey = properties.pickerApiKey;
	if (!isFalseOrEmpty_(properties.hostUri))
		newProperties.hostUri = properties.hostUri;
	let storage = new VolatileProperties(newProperties);
	return getAdminAuthService_(storage).getAuthorizationUrl(newProperties);
}
function getRedirectUri(): string {
	return (
		"https://script.google.com/macros/d/" +
		ScriptApp.getScriptId() +
		"/usercallback"
	);
}
function processNewAdminAuthResponse_(response) {
	let noOauthMessage: string =
		"Admin authorization response did not include OAuth 2.0 client information.";
	if (response.parameter == null) throw new Error(noOauthMessage);
	let oauthId: string = response.parameter.oauthId;
	let oauthSecret: string = response.parameter.oauthSecret;
	if (isFalseOrEmpty_(oauthId) || isFalseOrEmpty_(oauthSecret))
		throw new Error(noOauthMessage);
	let storage = new VolatileProperties();
	storage.setProperties(
		{
			oauthId: oauthId,
			oauthSecret: oauthSecret,
		},
		false
	);
	let service = getAdminAuthService_(storage);
	if (service.handleCallback(response)) {
		setProperty_("oauthSecret", oauthSecret);
		setProperty_("oauthId", oauthId);
		storage.deleteProperty("oauthId");
		storage.deleteProperty("oauthSecret");
		let token = service.getIdToken();
		if (!token) throw new Error("Unable to find administrator token");
		let adminId: string = getUserId_(token);
		if (isFalseOrEmpty_(adminId)) {
			throw new Error("Unable to determine administrator ID");
		} else {
			setProperty_("adminId", adminId);
		}
		createNewSpreadsheet_();
		return buildPage_(storage);
	} else {
		let errorMessage: string = response.parameter.error;
		if (isFalseOrEmpty_(errorMessage)) errorMessage = "Unknown error";
		let adminAuthError: string =
			"Unable to authorize administrative access: " + errorMessage;
		storage.setProperty("adminAuthError", adminAuthError);
		return buildPage_(storage);
	}
}
function processAdminAuthResponse_(response) {
	let storage = new VolatileProperties();
	let service = getAdminAuthService_(storage);
	if (service.handleCallback(response)) {
		let token = service.getIdToken();
		if (!token) throw new Error("Unable to get token");
		if (getUserId_(token) == null || getUserId_(token) != getAdminId_()) {
			storage.setProperties(
				{ adminAuthError: "Logged in user is not an administrator." },
				true
			);
			return buildPage_(storage);
		}
		if (response.parameter != null) {
			let newProperties: { [index: string]: string } = {};
			for (let property in response.parameter) {
				switch (property) {
					case "oauthId":
					case "oauthSecret":
					case "pickerApiKey":
					case "hostUri":
						newProperties[property] = response.parameter[property];
						break;
					default:
				}
			}
			setProperties_(newProperties);
		}
		return buildPage_(storage);
	} else {
		storage.setProperty(
			"adminAuthError",
			"Unable to authorize administrative access: access_denied"
		);
		return buildPage_(storage);
	}
}
function getUserAuthService_(storage: VolatileProperties = null) {
	let oauthId: string | null = getOauthId_();
	let callbackFunction: string = "processUserAuthResponse_";
	if (isFalseOrEmpty_(oauthId))
		throw new Error("OAuth 2.0 Client ID is not set");
	let oauthSecret: string | null = getOauthSecret_();
	if (isFalseOrEmpty_(oauthSecret))
		throw new Error("OAuth 2.0 Client secret is not set");
	if (storage == null) storage = new VolatileProperties();
	if (oauthId == null || oauthSecret == null) throw new Error();
	let oauth2Service = OAuth2.createService("userLogin")
		.setAuthorizationBaseUrl("https://accounts.google.com/o/oauth2/auth")
		.setTokenUrl("https://accounts.google.com/o/oauth2/token")
		.setClientId(oauthId)
		.setClientSecret(oauthSecret)
		.setCallbackFunction(callbackFunction)
		.setPropertyStore(storage)
		.setScope(
			"openid https://www.googleapis.com/auth/drive.appdata https://www.googleapis.com/auth/drive.file"
		)
		.setParam("access_type", "offline")
		.setParam("prompt", "consent");
	if (storage.getProperty("callbackUrl")) {
		console.log(
			"Changing callback from default to " +
				storage.getProperty("callbackUrl")
		);
		oauth2Service.setRedirectUri(storage.getProperty("callbackUrl"));
	} else {
		console.log("Using default callback url");
	}
	return oauth2Service;
}
function getUserAuthUrl(pageStorage: { [key: string]: string } | null): string {
	let storage = new VolatileProperties(pageStorage);
	return getUserAuthService_(storage).getAuthorizationUrl();
}
function processUserAuthResponse_(response) {
	let storage = new VolatileProperties();
	let service = getUserAuthService_(storage);
	if (service.handleCallback(response)) {
		let token = service.getIdToken();
		if (!token) throw new Error("Unable to get ID token");
		let userId: string = getUserId_(token);
		if (userId == null) throw new Error("Unable to obtain user ID");
		if (userId == getAdminId_()) storage.setProperty("adminUser", "true");
		createUserFile_(userId, storage);
		storage.setProperty("hasUserSpreadsheet", "true");
	} else {
		storage.setProperty(
			"userAuthError",
			"Unable to authorize user access: access_denied"
		);
	}
	return buildPage_(storage);
}
function getUserSpreadsheetId_(userId: string): string | null {
	if (userId == null)
		throw new Error("Unable to obtain spreadsheet for a null user ID");
	let users: { userId: string; userFileId: string }[] | null = getUsers_();
	if (users == null || users.length == 0) return null;
	let userEntries: { userId: string; userFileId: string }[] = users.filter(
		function (entry) {
			return entry.userId == userId;
		}
	);
	if (userEntries == null || userEntries.length == 0) return null;
	if (userEntries.length > 1)
		throw new Error("Multiple users found with the same ID");
	return userEntries[0].userFileId;
}
function getUserId_(id_token: string): string {
	if (id_token == null) throw new Error("ID token is null");
	else {
		let match = id_token.match(/\./g);
		if (!match || match.length != 2)
			throw new Error("Invalid ID token: " + id_token);
	}
	let id_body = id_token.split(/\./)[1];
	let id = JSON.parse(
		Utilities.newBlob(Utilities.base64Decode(id_body)).getDataAsString()
	);
	if (id.aud != getOauthId_())
		throw new Error("Invalid ID token: audience does not match");
	if (
		id.iss != "https://accounts.google.com" &&
		id.iss != "accounts.google.com"
	)
		throw new Error("Invalid ID token: issuer is not Google");
	let date = new Date();
	if (id.exp < Math.floor(date.getTime() / 1000))
		throw new Error("ID token has expired");
	// TODO #146: validate signature of id_token
	return id.sub;
}
function getUserLoginStatus(pageStorageJson: string): string | null {
	let pageStorage: { [key: string]: string } = JSON.parse(pageStorageJson);
	let storage = new VolatileProperties(pageStorage);
	let service = getUserAuthService_(storage);
	if (service.hasAccess()) {
		let token = service.getIdToken();
		if (!token) throw new Error("Unable to get ID token");
		let userId: string = getUserId_(token);
		if (userId == null) throw new Error("Unable to obtain user ID");
		if (userId == getAdminId_()) storage.setProperty("adminUser", "true");
		createUserFile_(userId, storage);
		storage.setProperty("hasUserSpreadsheet", "true");
		return JSON.stringify(storage.getProperties());
	} else {
		return null;
	}
}
/**
 * Create HTML output suitable for inclusion in another page
 * @param {string} filename Name of file containing HTML contents or template
 * @returns {string} Displayable HTML suitable for including within HTML output
 */
function include(filename: string, storage: VolatileProperties = null): string {
	let template = HtmlService.createTemplateFromFile(filename);
	if (storage == null) storage = new VolatileProperties();
	template.storage = storage;
	return template.evaluate().getContent();
}
function getDateString_(): string {
	let today = new Date();
	let year = today.getFullYear().toString();
	let month = (today.getMonth() + 1).toString();
	if (month.length == 1) month = "0" + month;
	let date = today.getDate().toString();
	if (date.length == 1) date = "0" + date;
	return year + month + date;
}
function eraseSpreadsheet_(): void {
	let spreadsheet = getSpreadsheet_();
	for (let metadata of spreadsheet.createDeveloperMetadataFinder().find())
		metadata.remove();
	let oldSheets = spreadsheet.getSheets();
	spreadsheet.insertSheet(0);
	for (let i = 0; i < oldSheets.length; i++) {
		spreadsheet.deleteSheet(oldSheets[i]);
	}
}
function createNewSpreadsheet_(): GoogleAppsScript.Spreadsheet.Spreadsheet {
	let spreadsheet = getSpreadsheet_();
	if (
		spreadsheet.getSheets().length > 1 ||
		spreadsheet.getSheetByName("Cover")
	)
		throw new Error("The mffer spreadsheet already exists.");
	let coverSheet = spreadsheet.getSheets()[0];
	coverSheet
		.setName("Cover")
		.getRange(1, 1)
		.setValue("This file is used by mffer. Do not edit or delete it.");
	for (let sheet of spreadsheet.getSheets()) {
		if (sheet.getSheetId() != coverSheet.getSheetId()) {
			spreadsheet.deleteSheet(sheet);
		}
	}
	spreadsheet.addDeveloperMetadata(
		"mffer-version",
		"0.1",
		SpreadsheetApp.DeveloperMetadataVisibility.DOCUMENT
	);
	spreadsheet.addDeveloperMetadata(
		"mffer-cover-sheet",
		coverSheet.getSheetId().toString(),
		SpreadsheetApp.DeveloperMetadataVisibility.DOCUMENT
	);
	createUsersFile_();
	return spreadsheet;
}
/**
 * Make changes to the requested spreadsheet. Requires drive or spreadsheets
 * scope, or may use drive.file if the spreadsheet was "selected or created" by
 * this app, or drive.appdata if the file is "hidden" in the /appDataFolder
 * folder.
 * @param spreadsheetId ID of the spreadsheet to change
 * @param accessToken authorizing changes to the spreadsheet
 * @param ValueRange object describing the range to replace and the values to
 * use
 * @returns the server response as a string
 */
function updateRemoteSpreadsheet(
	spreadsheetId: string,
	accessToken: string,
	ValueRange: any
): string {
	let url: string =
		"https://sheets.googleapis.com/v4/spreadsheets/" +
		spreadsheetId +
		"/values:batchUpdate";
	let request: GoogleAppsScript.URL_Fetch.Payload = {
		valueInputOption: "RAW",
		data: [ValueRange],
		includeValuesInResponse: false,
	};
	let params: GoogleAppsScript.URL_Fetch.URLFetchRequestOptions = {
		method: "post",
		contentType: "application/json",
		headers: {
			Authorization: "Bearer " + accessToken,
		},
		payload: JSON.stringify(request),
		muteHttpExceptions: true,
	};
	let response = UrlFetchApp.fetch(url, params);
	return response.getContentText();
}
/**
 * Set the authenticated user to be the authenticated administrator to avoid
 * the need for multiple logins.
 * @param storage persistent storage object containing authentication
 * information
 */
function setAdminToUser_(storage: VolatileProperties): void {
	if (storage.getProperty("adminAuthError")) return;
	if (
		storage.getProperty("oauth2.adminLogin") == null &&
		storage.getProperty("oauth2.userLogin") != null
	) {
		storage.setProperty(
			"oauth2.adminLogin",
			storage.getProperty("oauth2.userLogin")
		);
		let service = getAdminAuthService_(storage);
		if (service.hasAccess()) {
			let token = service.getIdToken();
			if (!token) throw new Error("Unable to get ID token");
			if (getUserId_(token) == getAdminId_()) return;
		}
	}
	storage
		.deleteAllProperties()
		.setProperty("adminAuthError", "No administrator is logged in.");
}
/**
 * Converts a column number to an A1-formatted column label
 * @param colNum The number of the column in a spreadsheet; the first column is
 * number 1
 * @returns the A1-formatted column label; the first column is A, the 28th is AB
 */
function numToCol_(colNum: number): string | null {
	let colName: string | null = null;
	let colBaseLabels: string = "ABFCDEFGHIJKLMNOPQRSTUVWXYZ";
	let colGroup = Math.floor(colNum / colBaseLabels.length);
	if (colGroup > 0) {
		colName = numToCol_(colGroup);
	}
	colName += colBaseLabels[colNum % colBaseLabels.length];
	return colName;
}
function importNewData(newText: string, storageJson: string): void {
	let storage = new VolatileProperties(JSON.parse(storageJson));
	setAdminToUser_(storage);
	if (storage.getProperty("adminAuthError"))
		throw new Error(
			"Authorization error: " + storage.getProperty("adminAuthError")
		);
	let authService = getAdminAuthService_(storage);
	if (!authService.hasAccess()) {
		throw new Error("Access is not available");
	}
	let spreadsheet = getSpreadsheet_();
	let dataSheet = getDataSheet_();
	if (dataSheet != null) {
		dataSheet.copyTo(spreadsheet);
		dataSheet.clear();
	} else {
		dataSheet = spreadsheet.insertSheet(1);
		spreadsheet.addDeveloperMetadata(
			"mffer-data-sheet",
			dataSheet.getSheetId().toString(),
			SpreadsheetApp.DeveloperMetadataVisibility.DOCUMENT
		);
	}
	let dataSheetName: string = "mffer - " + getDateString_();
	dataSheet.setName(dataSheetName);
	let newData = Utilities.parseCsv(newText, "|");
	if (newData.length == 0) return;
	let dataRange = dataSheet.getRange(1, 1, newData.length, newData[0].length);
	dataRange.setValues(newData);
}
function removeAppData_(): void {
	let response = UrlFetchApp.fetch(
		"https://www.googleapis.com/drive/v3/files?spaces=appDataFolder&pageSize=1000",
		{
			method: "get",
			muteHttpExceptions: true,
			contentType: "application/json",
			headers: {
				Authorization: "Bearer " + ScriptApp.getOAuthToken(),
			},
		}
	);
	if (response.getResponseCode() != 200) {
		let message = "Response:\n";
		message += `${JSON.stringify(response.getAllHeaders())}\n`;
		message += `${JSON.stringify(response.getContentText())}\n`;
		throw new Error("Unable to access drive:\n" + message);
	}
	let files: any[] = JSON.parse(response.getContentText()).files;
	let fileUrls: GoogleAppsScript.URL_Fetch.URLFetchRequest[] = [];
	if (files.length > 0) {
		for (let file of files) {
			if (file.id) {
				fileUrls.push({
					url: "https://www.googleapis.com/drive/v3/files/" + file.id,
					method: "delete",
					muteHttpExceptions: true,
					headers: {
						Authentication: "Bearer " + ScriptApp.getOAuthToken(),
					},
				});
			}
		}
	}
	if (fileUrls.length > 0) {
		let deleteResponses = UrlFetchApp.fetchAll(fileUrls);
		for (let deleteResponseIndex in deleteResponses) {
			if (deleteResponses[deleteResponseIndex].getResponseCode() != 200) {
				let errorMessage =
					"Unable to delete file:\n" +
					`${JSON.stringify(
						deleteResponses[deleteResponseIndex].getAllHeaders()
					)}\n` +
					`${JSON.stringify(
						deleteResponses[deleteResponseIndex].getContentText()
					)}`;
			}
		}
	}
}
function createUsersFile_(): void {
	let uploadUrl =
		"https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
	let fileInfo = {
		name: "mffer.json",
		parents: ["appDataFolder"],
		description: "mffer application data",
	};
	let fileData: string = JSON.stringify({
		userData: [],
	});
	let mimeBoundary = "ThisIsTheBoundary";
	let uploadRequest: string =
		`--${mimeBoundary}\n` +
		`Content-Type: application/json\n\n` +
		`${JSON.stringify(fileInfo)}\n` +
		`--${mimeBoundary}\n` +
		`Content-Type: application/json\n\n` +
		`${fileData}\n` +
		`--${mimeBoundary}--\n`;
	let uploadParams: GoogleAppsScript.URL_Fetch.URLFetchRequestOptions = {
		method: "post",
		muteHttpExceptions: true,
		contentType: "multipart/related; boundary=" + mimeBoundary,
		headers: {
			Authorization: "Bearer " + ScriptApp.getOAuthToken(),
		},
		payload: uploadRequest,
	};
	let uploadResponse = UrlFetchApp.fetch(uploadUrl, uploadParams);
	if (uploadResponse.getResponseCode() != 200) {
		let errorData =
			"Headers:\n" +
			JSON.stringify(uploadResponse.getAllHeaders()) +
			"\n" +
			"Content:\n" +
			uploadResponse.getContentText();
		throw new Error("Unable to create users file:\n" + errorData);
	}
	let usersFileId: string = JSON.parse(uploadResponse.getContentText()).id;
	getSpreadsheet_().addDeveloperMetadata(
		"mffer-users-sheet",
		usersFileId,
		SpreadsheetApp.DeveloperMetadataVisibility.DOCUMENT
	);
	setProperty_("usersFileId", usersFileId);
}
function addUserFile_(userId: string, userFileId: string) {
	let usersFileId = getUsersFileId_();
	let userData: { userId: string; userFileId: string }[] | null = getUsers_();
	if (!userData) userData = [];
	let existingUsers = userData.filter(function (entry) {
		return entry.userId == userId;
	});
	if (existingUsers.length > 1)
		throw new Error(
			"Multiple users already found with the same user ID " + userId
		);
	if (existingUsers.length == 1) {
		existingUsers[0].userFileId = userFileId;
	} else {
		userData.push({ userId, userFileId });
	}
	let fileData = JSON.stringify({
		userData: userData,
	});
	let uploadResponse = UrlFetchApp.fetch(
		`https://www.googleapis.com/upload/drive/v3/files/${usersFileId}?uploadType=media`,
		{
			method: "patch",
			muteHttpExceptions: true,
			contentType: "application/json",
			headers: {
				Authorization: "Bearer " + ScriptApp.getOAuthToken(),
			},
			payload: fileData,
		}
	);
	if (uploadResponse.getResponseCode() != 200) {
		let errorData =
			"Headers:\n" +
			JSON.stringify(uploadResponse.getAllHeaders()) +
			"\n" +
			"Content:\n" +
			uploadResponse.getContentText();
		throw new Error("Unable to update users file:\n" + errorData);
	}
}
function createUserFile_(
	userId: string,
	storage: VolatileProperties
): string | null {
	let auth = getUserAuthService_(storage);
	if (
		auth.hasAccess() &&
		getScopes_(storage).includes(
			"https://www.googleapis.com/auth/drive.appdata"
		)
	) {
		let userFileId = getUserSpreadsheetId_(userId);
		if (!isFalseOrEmpty_(userFileId)) return userFileId;
		let uploadUrl =
			"https://www.googleapis.com/upload/drive/v3/files?uploadType=multipart";
		let fileInfo = {
			name: "mffer.json",
			parents: ["appDataFolder"],
			description: "mffer user data",
		};
		let fileData: string = JSON.stringify({
			userData: [],
		});
		let mimeBoundary = "ThisIsTheBoundary";
		let uploadRequest: string =
			`--${mimeBoundary}\n` +
			`Content-Type: application/json\n\n` +
			`${JSON.stringify(fileInfo)}\n` +
			`--${mimeBoundary}\n` +
			`Content-Type: application/json\n\n` +
			`${fileData}\n` +
			`--${mimeBoundary}--\n`;
		let uploadParams: GoogleAppsScript.URL_Fetch.URLFetchRequestOptions = {
			method: "post",
			muteHttpExceptions: true,
			contentType: "multipart/related; boundary=" + mimeBoundary,
			headers: {
				Authorization: "Bearer " + ScriptApp.getOAuthToken(),
			},
			payload: uploadRequest,
		};
		let uploadResponse = UrlFetchApp.fetch(uploadUrl, uploadParams);
		if (uploadResponse.getResponseCode() != 200) {
			let errorData =
				"Headers:\n" +
				JSON.stringify(uploadResponse.getAllHeaders()) +
				"\n" +
				"Content:\n" +
				uploadResponse.getContentText();
			throw new Error("Unable to create user file:\n" + errorData);
		}
		userFileId = JSON.parse(uploadResponse.getContentText()).id;
		if (!userFileId) userFileId = "";
		addUserFile_(userId, userFileId);
		return userFileId;
	} else {
		throw new Error("No permission to create user file.");
	}
}
function getScopes_(storage: VolatileProperties): string[] {
	getUserAuthService_(storage);
	let scopeString = JSON.parse(storage.getProperty("oauth2.userLogin")).scope;
	return scopeString.split(" ");
}
/**
 * Allow referring to an individual sheet by ID rather than name (which
 * may change).
 * modified from an excellent Stack Overflow answer at
 * https://stackoverflow.com/a/51789725
 */
function getSheetById_(
	spreadsheet: GoogleAppsScript.Spreadsheet.Spreadsheet,
	gid: number
): GoogleAppsScript.Spreadsheet.Sheet | null {
	if (!spreadsheet) {
		spreadsheet = getSpreadsheet_();
	}
	for (var sheet of spreadsheet.getSheets()) {
		if (sheet.getSheetId() == gid) {
			return sheet;
		}
	}
	return null;
}

/**
 * @summary Retrieves the data for the webapp
 * @description Allows using a single SpreadsheetApp data request to obtain
 * all necessary startup information for the web app. Depends upon
 * spreadsheet including an appropriate range for consolidating all
 * this information prior to starting the web app.
 *
 * Because Sheets ranges are normally accessed as a double array of
 * array[row][column], which is the opposite of the way I consider
 * intuitive (array[column][row]), we additionally transpose the
 * returned matrix.
 *
 * As of this version, the relevant data are in the "Shadowland
 * Working Sheet" sheet (gid 1514300109), columns AG:BL. Column
 * titles are in database[column][0]; these include:
 *
 * 0 Floor Number
 * 1 Floor Type
 * 2 Floor Opponents
 * 3 Floor Opponent Numbers
 * 4 Floor Reward 1
 * 5 Floor Reward 2
 * 6 Floor Reward 3
 * 7 Opponent Teams
 * 8 Opponent Restriction-Meeting Character-Uniform Combos
 * 9 Opponent Prior Defeating Teams
 * 10 Completed Floors This Week
 * 11 Used Characters This Week
 * 12 Characters
 * 13 Character Tier
 * 14 Character Rank
 * 15 Character Mastery
 * 16 Character Default Uniform
 * 17 Character Current Uniform
 * 18 Character Available Uniforms
 * 19 Character Uniform Combinations
 * 20 Character Uniform Combination Gender
 * 21 Character Uniform Combination Side
 * 22 Character Uniform Combination Type
 * 23 Character Uniform Combination Damage
 * 24 Character Uniform Combination Attack
 * 25 character Uniform Combination Quality
 * 26 Preference Name
 * 27 Preference Value
 * 28 Sheet Floor
 * 29 Sheet Opponents
 * 30 Sheet Opponent Choice
 * 31 Sheet Teammembers
 */
function getWebappDatabase(): any[][] | null {
	var sheet: GoogleAppsScript.Spreadsheet.Sheet | null = getDataSheet_();
	if (sheet == null) return null;
	var rows: number = sheet.getDataRange().getHeight();
	var range: any[][] = sheet.getSheetValues(1, 33, rows, 32);
	// See above; range[0] is the row of column headers, so
	// range[0].map( function ( header, column ) { } ) is equivalent to
	// foreach ( column in columns ) {}.
	// We then iterate over the rows and return the entry in each row for that
	// column, so that the final result switches the columns and rows and
	// allows us to refer to each cell as newrange[columnnumber][rownumber].
	// TODO: #118 make a function that returns the necessary value rather than wasting the time inverting the matrix
	return range[0].map(function (_: any, column: number) {
		return range.map(function (row: any[]) {
			return row[column];
		});
	});
}
function getSheet_(
	mfferSheet: string
): GoogleAppsScript.Spreadsheet.Sheet | null {
	let spreadsheet = getSpreadsheet_();
	if (spreadsheet == null) return null;
	let metadata = spreadsheet
		.createDeveloperMetadataFinder()
		.withLocationType(
			SpreadsheetApp.DeveloperMetadataLocationType.SPREADSHEET
		)
		.withVisibility(SpreadsheetApp.DeveloperMetadataVisibility.DOCUMENT)
		.withKey(mfferSheet)
		.find();
	if (metadata == null || metadata.length == 0 || metadata[0] == null)
		return null;
	let value = metadata[0].getValue();
	if (value == null || value.trim() == "") return null;
	let sheet: GoogleAppsScript.Spreadsheet.Sheet | null = null;
	try {
		sheet = getSheetById_(spreadsheet, Number(value));
	} catch (exception) {
		return null;
	}
	return sheet;
}
function getDataSheet_(): GoogleAppsScript.Spreadsheet.Sheet | null {
	return getSheet_("mffer-data-sheet");
}
function getUsersSheet_(): GoogleAppsScript.Spreadsheet.Sheet | null {
	return getSheet_("mffer-users-sheet");
}
function getUsersFileId_(): string | null {
	return getProperty_("usersFileId");
}
function getUsers_(): { userId: string; userFileId: string }[] | null {
	let usersFileId = getUsersFileId_();
	if (!usersFileId) return null;
	let response = UrlFetchApp.fetch(
		"https://www.googleapis.com/drive/v3/files/" +
			usersFileId +
			"?alt=media",
		{
			method: "get",
			headers: {
				Authorization: "Bearer " + ScriptApp.getOAuthToken(),
			},
		}
	);
	return JSON.parse(response.getContentText()).userData;
}
/**
 * Sets preferences from the webapp to the spreadsheet version
 * of the calculator. Takes array of booleans matching the
 * checkboxes on the forms (which are the same as in the
 * database array column 27).
 *
 * (Even though that sheet is no longer being used and therefore doesn't
 * require synchronization, this is a currently acceptable way to store
 * the preferences between instantiations. TODO: At some point can likely just
 * use the working sheet.)
 */
function saveNewPreferences(preferences) {
	let sheet = getSheetById_(getSpreadsheet_(), 1315797114);
	if (!sheet) throw new Error("Unable to find sheet");
	else sheet.getRange(15, 3, 5).setValues(preferences);
}

/**
 * Saves a new row to the Shadowland record. The entry should be an array already
 * given in the appropriate order:
 * [ floor, mode, opponent team 1, opponent team 2, opponent team 3, selected opponent team,
 *   winning team ]
 */
function saveShadowlandEntry(entry) {
	// As webapps aren't permitted to pass a time, we find it here
	entry.unshift(new Date());
	let sheet = getSheetById_(getSpreadsheet_(), 1930936724);
	if (!sheet) throw new Error("Unable to get sheet");
	else sheet.appendRow(entry);
}

/**
 * Sets the "current floor" in the database
 * (i.e., the old sheet, see comments for saveNewPreferences())
 */
function saveFloorNumber(floorNumber) {
	let sheet = getSheetById_(getSpreadsheet_(), 1315797114);
	if (!sheet) throw new Error("Unable to get sheet");
	else sheet.getRange("A2").setValue(floorNumber);
}

function csvToTable(text: string, storageJson: string) {
	let storage = new VolatileProperties(JSON.parse(storageJson));
	let returnContent: string | null = null;
	setAdminToUser_(storage);
	let adminService = getAdminAuthService_(storage);

	var csvArray = Utilities.parseCsv(text, "|");
	var returnTable = HtmlService.createHtmlOutput("<table>");
	for (var row of csvArray) {
		returnTable.append("<tr>");
		for (var cell of row) {
			returnTable.append("<td>");
			returnTable.appendUntrusted(cell);
			returnTable.append("</td>");
		}
		returnTable.append("</tr>");
	}
	returnTable.append("</table>");
	let returnStorage: string =
		'<div id="mffer-admin-filechooser-pending-storage" class="start-inactive">' +
		JSON.stringify(storage.getProperties()) +
		"</div>";

	return returnTable.getContent() + returnStorage;
}
