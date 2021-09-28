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
 * script, effectively resetting the deployment. Private function specified with
 * the trailing _ cannot be run from the client side or easily selected in the
 * Apps Script IDE. To run (and reset all properties), open in the Apps Script
 * IDE, uncomment the below line starting with "function", press "Save", select
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
/**
 * The basic webapp-enabling function responding to the HTTP GET request
 * @returns {GoogleAppsScript.HTML.HtmlOutput} web page appropriate to the
 * request
 */
function doGet(): GoogleAppsScript.HTML.HtmlOutput {
	return buildPage();
}
/**
 * Construct the web page from the Index.html template
 * @returns Apps Script-compatible web page
 */
function buildPage(
	storage: VolatileProperties = null
): GoogleAppsScript.HTML.HtmlOutput {
	let properties = getProperties_();
	let contents = include("Index.html", storage);
	let page = HtmlService.createHtmlOutput(contents)
		.addMetaTag(
			"viewport",
			"width=device-width, initial-scale=1, shrink-to-fit=no"
		)
		.setTitle("mffer: Marvel Future Fight Extraction & Reporting");
	if (properties != null && properties.getProperty("hostUri") != null) {
		page.setXFrameOptionsMode(HtmlService.XFrameOptionsMode.ALLOWALL);
	}
	return page;
}
function getConfig() {
	return {
		oauthId: getOauthId_(),
		oauthSecret: hasOauthSecret_(),
		pickerApiKey: getPickerApiKey_(),
	};
}
function isConfigured(): boolean {
	if (isFalseOrEmpty(getOauthId_()) || isFalseOrEmpty(getOauthSecret_())) {
		return false;
	} else return true;
}
function getProperty_(propertyName: string): string {
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
function getOauthId_(): string {
	return getProperty_("oauthId");
}
function getOauthSecret_(): string {
	return getProperty_("oauthSecret");
}
function getPickerApiKey_(): string {
	return getProperty_("pickerApiKey");
}
function hasOauthSecret_(): boolean {
	return getOauthSecret_() != null;
}
function setProperty_(propertyName: string, propertyValue: string) {
	var properties = getProperties_();
	if (properties == null) {
		throw "Unable to access script properties";
	}
	properties.setProperty(propertyName, propertyValue);
}
function getAdminAuthService_(storage: VolatileProperties = null) {
	let oauthId: string = getOauthId_();
	let callbackFunction: string = "processAdminAuthResponse";
	if (isFalseOrEmpty(oauthId) && storage != null) {
		oauthId = storage.getProperty("oauthId");
		callbackFunction = "processNewAdminAuthResponse";
	}
	if (isFalseOrEmpty(oauthId))
		throw new Error("OAuth 2.0 Client ID is not set");
	let oauthSecret: string = getOauthSecret_();
	if (isFalseOrEmpty(oauthSecret) && storage != null) {
		oauthSecret = storage.getProperty("oauthSecret");
		callbackFunction = "processNewAdminAuthResponse";
	}
	if (isFalseOrEmpty(oauthSecret))
		throw new Error("OAuth 2.0 Client secret is not set");

	return OAuth2.createService("adminLogin")
		.setAuthorizationBaseUrl("https://accounts.google.com/o/oauth2/auth")
		.setTokenUrl("https://accounts.google.com/o/oauth2/token")
		.setClientId(oauthId)
		.setClientSecret(oauthSecret)
		.setCallbackFunction(callbackFunction)
		.setPropertyStore(storage)
		.setScope("https://www.googleapis.com/auth/drive.file")
		.setParam("access_type", "offline")
		.setParam("prompt", "consent");
}
function isFalseOrEmpty(check: string | boolean | null): boolean {
	if (!check || check.toString().trim() === "") return true;
	return false;
}
function getAdminAuthUrl(oauthId: string = null, oauthSecret: string = null) {
	if (isFalseOrEmpty(oauthId) && isFalseOrEmpty(oauthSecret))
		return getAdminAuthService_().getAuthorizationUrl();
	if (isFalseOrEmpty(oauthId)) oauthId = getProperty_("oauthId");
	if (isFalseOrEmpty(oauthSecret)) oauthSecret = getProperty_("oauthSecret");
	let storage = new VolatileProperties();
	storage.setProperties(
		{
			oauthId: oauthId,
			oauthSecret: oauthSecret,
		},
		false
	);
	return getAdminAuthService_(storage).getAuthorizationUrl(
		storage.getProperties()
	);
}
function getRedirectUri(): string {
	return (
		"https://script.google.com/macros/d/" +
		ScriptApp.getScriptId() +
		"/usercallback"
	);
}
function processNewAdminAuthResponse(request) {
	let noOauthMessage: string =
		"Admin authorization response did not include OAuth 2.0 client information.";
	if (request.parameter == null) throw new Error(noOauthMessage);
	let oauthId: string = request.parameter.oauthId;
	let oauthSecret: string = request.parameter.oauthSecret;
	if (oauthId == null || oauthSecret == null) throw new Error(noOauthMessage);

	let storage = new VolatileProperties();
	storage.setProperties(
		{
			oauthId: oauthId,
			oauthSecret: oauthSecret,
		},
		false
	);
	let service = getAdminAuthService_(storage);
	if (service.handleCallback(request)) {
		setProperty_("oauthSecret", oauthSecret);
		setProperty_("oauthId", oauthId);
		storage.deleteProperty("oauthId").deleteProperty("oauthSecret");
		return buildPage(storage);
	} else {
		let errorMessage: string = request.parameter.error;
		if (errorMessage == null || errorMessage.toString().trim() === "")
			errorMessage = "Unknown error";
		let adminAuthError: string =
			"Unable to authorize administrative access: " + errorMessage;
		storage.setProperty("adminAuthError", adminAuthError);
		return buildPage(storage);
	}
}
function processAdminAuthResponse(request) {
	let storage = new VolatileProperties();
	let service = getAdminAuthService_(storage);
	if (service.handleCallback(request)) {
		return buildPage(storage);
	} else {
		return HtmlService.createHtmlOutput("Authorization denied.");
	}
}
function getUserAuthService_(storage: VolatileProperties = null) {
	let oauthId: string = getOauthId_();
	let callbackFunction: string = "processUserAuthResponse";
	if (isFalseOrEmpty(oauthId))
		throw new Error("OAuth 2.0 Client ID is not set");
	let oauthSecret: string = getOauthSecret_();
	if (isFalseOrEmpty(oauthSecret))
		throw new Error("OAuth 2.0 Client secret is not set");
	return OAuth2.createService("userLogin")
		.setAuthorizationBaseUrl("https://accounts.google.com/o/oauth2/auth")
		.setTokenUrl("https://accounts.google.com/o/oauth2/token")
		.setClientId(oauthId)
		.setClientSecret(oauthSecret)
		.setCallbackFunction(callbackFunction)
		.setPropertyStore(storage)
		.setScope("https://www.googleapis.com/auth/drive.file")
		.setParam("access_type", "offline")
		.setParam("prompt", "consent");
}
function getUserAuthUrl(pageStorage: { [key: string]: string }): string {
	let storage = new VolatileProperties(pageStorage);
	return getUserAuthService_(storage).getAuthorizationUrl();
}
function processUserAuthResponse(request) {
	let storage = new VolatileProperties();
	let service = getUserAuthService_(storage);
	if (service.handleCallback(request)) {
		return buildPage(storage);
	} else {
		return HtmlService.createHtmlOutput("Authorization denied.");
	}
}
function getUserLoginStatus(pageStorageJson: string): string {
	let pageStorage: { [key: string]: string } = JSON.parse(pageStorageJson);
	let storage = new VolatileProperties(pageStorage);
	if (getUserAuthService_(storage).hasAccess())
		return JSON.stringify(storage.getProperties());
	else {
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
function getDateString(): string {
	let today = new Date();
	let year = today.getFullYear().toString();
	let month = today.getMonth().toString();
	if (month.length == 1) month = "0" + month;
	let date = today.getDate().toString();
	if (date.length == 1) date = "0" + date;
	return year + month + date;
}
function createNewSpreadsheet(): GoogleAppsScript.Spreadsheet.Spreadsheet {
	let spreadsheet = getSpreadsheet_();
	let coverSheet = spreadsheet.insertSheet("Cover", 0);
	coverSheet
		.getRange(1, 1)
		.setValue("This file is used by mffer.Do not edit or delete it.");
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
	return spreadsheet;
}
function importNewData(newText: string): void {
	let spreadsheet = getSpreadsheet_();
	let dataSheet = getDataSheet();
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
	dataSheet.setName("mffer - " + getDateString());
	let newData = Utilities.parseCsv(newText, "|");
	if (newData.length == 0) return;
	let dataRange = dataSheet.getRange(1, 1, newData.length, newData[0].length);
	dataRange.setValues(newData);
}
/**
 * Allow referring to an individual sheet by ID rather than name (which
 * may change).
 * modified from an excellent Stack Overflow answer at
 * https://stackoverflow.com/a/51789725
 */
function getSheetById(
	spreadsheet: GoogleAppsScript.Spreadsheet.Spreadsheet,
	gid: number
): GoogleAppsScript.Spreadsheet.Sheet {
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
function getWebappDatabase(): any[][] {
	var sheet: GoogleAppsScript.Spreadsheet.Sheet = getDataSheet();
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
function getDataSheet(): GoogleAppsScript.Spreadsheet.Sheet {
	let spreadsheet = getSpreadsheet_();
	if (spreadsheet == null) return null;
	let metadata = spreadsheet
		.createDeveloperMetadataFinder()
		.withLocationType(
			SpreadsheetApp.DeveloperMetadataLocationType.SPREADSHEET
		)
		.withVisibility(SpreadsheetApp.DeveloperMetadataVisibility.DOCUMENT)
		.withKey("mffer-data-sheet")
		.find();
	if (
		metadata == null ||
		metadata.length == 0 ||
		metadata[0] == null ||
		metadata[0].getValue() == null ||
		metadata[0].getValue().trim() == ""
	)
		return null;
	let sheet: GoogleAppsScript.Spreadsheet.Sheet = null;
	try {
		sheet = getSheetById(spreadsheet, Number(metadata[0].getValue()));
	} catch (exception) {
		return null;
	}
	if (sheet == null) return null;
	return sheet;
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
	getSheetById(getSpreadsheet_(), 1315797114)
		.getRange(15, 3, 5)
		.setValues(preferences);
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
	getSheetById(getSpreadsheet_(), 1930936724).appendRow(entry);
}

/**
 * Sets the "current floor" in the database
 * (i.e., the old sheet, see comments for saveNewPreferences())
 */
function saveFloorNumber(floorNumber) {
	getSheetById(getSpreadsheet_(), 1315797114)
		.getRange("A2")
		.setValue(floorNumber);
}

function csvToTable(text) {
	var csvArray = Utilities.parseCsv(text, "|");
	var returnHtml = HtmlService.createHtmlOutput("<table>");
	for (var row of csvArray) {
		returnHtml.append("<tr>");
		for (var cell of row) {
			returnHtml.append("<td>");
			returnHtml.appendUntrusted(cell);
			returnHtml.append("</td>");
		}
		returnHtml.append("</tr>");
	}
	returnHtml.append("</table>");

	return returnHtml.getContent();
}