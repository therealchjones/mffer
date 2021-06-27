/**
 * Returns the spreadsheet object for the Marvel Future Fight Google Sheet where
 * data are stored.
 */
function getSpreadsheet() {
	return SpreadsheetApp.openById(spreadsheetId);
}

/**
 * The basic webapp enabling function responding to the HTTP GET request
 */
function doGet(e) {
	var properties = PropertiesService.getScriptProperties();
	if (properties == null || properties.getProperty("spreadsheet") == null) {
		return CreateNewSpreadsheet();
	}
	return HtmlService.createTemplateFromFile("Page.html")
		.evaluate()
		.addMetaTag(
			"viewport",
			"width=device-width, initial-scale=1, shrink-to-fit=no"
		)
		.setTitle("Marvel Future Fight Extraction & Reporting");
}

function CreateNewSpreadsheet() {
	return HtmlService.createTemplateFromFile("mff-upload.html")
		.evaluate()
		.addMetaTag(
			"viewport",
			"width=device-width, initial-scale=1, shrink-to-fit=no"
		)
		.setTitle("Marvel Future Fight Extraction & Reporting");
}

/**
 * Allow inclusion of HTML from another file to allow
 * separate structure/style/script/etc.
 */
function include(filename) {
	return HtmlService.createHtmlOutputFromFile(filename).getContent();
}

/**
 * Allow referring to an individual sheet by ID rather than name (which
 * may change).
 * modified from an excellent Stack Overflow answer at
 * https://stackoverflow.com/a/51789725
 */
function getSheetById(spreadsheet, gid) {
	if (!spreadsheet) {
		spreadsheet = getSpreadsheet();
	}
	for (var sheet of spreadsheet.getSheets()) {
		if (sheet.getSheetId() == gid) {
			return sheet;
		}
	}
	// apparently it doesn't exist
	throw "Sheet " + gid + " not found.";
}

/**
 * Allows using a single SpreadsheetApp data request to obtain
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
 *
 */
function getWebappDatabase() {
	var sheet = getSheetById(getSpreadsheet(), "1514300109");
	var rows = sheet.getDataRange().getHeight();
	var range = sheet.getSheetValues(1, 33, rows, 32);
	return range[0].map(function (column) {
		return range.map(function (row) {
			return row[column];
		});
	});
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
	getSheetById(getSpreadsheet(), "1315797114")
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
	getSheetById(getSpreadsheet(), "1930936724").appendRow(entry);
}

/**
 * Sets the "current floor" in the database
 * (i.e., the old sheet, see comments for saveNewPreferences())
 */
function saveFloorNumber(floorNumber) {
	getSheetById(getSpreadsheet(), "1315797114")
		.getRange("A2")
		.setValue(floorNumber);
}

function uploadNewMffData() {
	showUploadDialog();
	var csvString = "";
	var csvArray = Utilities.parseCsv(csvString, "|");
}

function showSidebar() {
	var html = HtmlService.createHtmlOutputFromFile("mff-sidebar.html")
		.setTitle("Marvel Future Fight")
		.setWidth(300);
	SpreadsheetApp.getUi().showSidebar(html);
}

function showUploadDialog() {
	var html = HtmlService.createHtmlOutputFromFile("mff-upload.html");
	html.setTitle("Upload file");
	SpreadsheetApp.getUi().showModalDialog(html, "Upload file");
}

function insertNow() {
	var now = new Date();
	var cell = SpreadsheetApp.getActiveSpreadsheet().getActiveCell();
	cell.setValue(now);
}

function csvToTable(text) {
	var csvArray = Utilities.parseCsv(text);
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

/***** helper functions *****/

function activateSheet(tabname) {
	var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();
	var sheet = spreadsheet.getSheetByName(tabname);
	sheet.activate();
}

function activateSection(name) {
	// to define a section in a sheet, make a cell that includes the name in square brackets, like [sectionname]
	var sheet = SpreadsheetApp.getActiveSheet();
	var section = sheet.createTextFinder("[" + name + "]");
	cell = section.findNext();
	if (!cell) {
		ui = SpreadsheetApp.getUi();
		ui.alert(
			"Error",
			"The section " + name + " was not found.",
			ui.ButtonSet.OK
		);
		return false;
	} else {
		cell.activateAsCurrentCell();
		return true;
	}
}

function rebuildCharacterUniformValidation() {
	/* for each character on the Characters sheet,
	 * set the data validation for the accompanying "Equipped Uniform"
	 * cell to use that character's uniforms. Because cells change with sorting
	 * and such, this will use a generated list of values rather than
	 * a fixed range.
	 */
	/* consider making this an auto-updated thing with daily/weekly reset
	 * and/or a menu item
	 */

	// Note that this relies on the "working sheet" and "character sheet"
	// character lists to be in the same order (which currently they automatically are)

	var spreadsheet = SpreadsheetApp.openById("[spreadsheet ID not specified]");
	var characterSheet = getSheetById(spreadsheet, 1392219691); // the "Characters" sheet
	var workingSheet = getSheetById(spreadsheet, 1291082607); // the "Characters Working Sheet" sheet

	// find the range of "Equipped Uniform" cells to validate
	var toValidateRange = characterSheet.getRange("$B$2:$B");
	// find the range of available uniforms to use as validation
	var validationFinder = spreadsheet
		.createTextFinder("Uniform 1")
		.matchCase(true)
		.matchEntireCell(true)
		.startFrom(workingSheet.getRange("$A$1"));
	var validationStartColumn = validationFinder.findNext().getColumn();

	var rules = toValidateRange.getDataValidations();
	var startRow = toValidateRange.getRow();
	var rows = toValidateRange.getHeight();
	for (var i = startRow; i < startRow + rows; i++) {
		var validationRange = workingSheet.getRange(
			i,
			validationStartColumn,
			1,
			13
		);
		var rule = SpreadsheetApp.newDataValidation()
			.setAllowInvalid(false)
			.requireValueInRange(validationRange)
			.build();
		rules[i - startRow][0] = rule;
	}
	toValidateRange.setDataValidations(rules);
}

/*** deprecated functions; gotten rid of these or moved them to the standalone webapp project ***/

function syncFloorData(
	floor,
	opp1,
	opp2,
	opp3,
	selected,
	team1,
	team2,
	team3,
	preferences
) {
	var spreadsheet = SpreadsheetApp.openById("[spreadsheet ID not specified]");
	var calculator = getSheetById(spreadsheet, 1315797114);
	var preferenceBoxes = calculator.getRange(15, 3, 5);
	preferenceBoxes.setValues(preferences);
}

function copyTeam(column) {
	var calculator = SpreadsheetApp.getActiveSheet();
	var selected = calculator.getCurrentCell();

	var regex = new RegExp("^" + column + "([0-9]+)$");
	var rownum = selected.getA1Notation().replace(regex, "$1");
	if (isNaN(rownum) || rownum <= 14) {
		return false;
	}

	var team = selected
		.getValue()
		.replace(/\[Floor.*$/, "")
		.split("+")[0]
		.trim();
	if (team == "") {
		return false;
	}

	var cols = ["D", "G", "J"];
	var solos = [0, 0, 0];
	var teammembers = team.split("/");
	for (var i = 0; i < teammembers.length; i++) {
		teammembers[i] = teammembers[i].trim();
		if (teammembers[i].endsWith("(solo)")) {
			teammembers[i] = teammembers[i].replace(/\(solo\)$/, "").trim();
			solos[i] = 1;
		}
	}
	if (solos[0] + solos[1] + solos[2] > 1) {
		solos = [0, 0, 0];
	}

	for (var i = 0; i < 3; i++) {
		if (teammembers[i] == null || teammembers[i] == "") {
			calculator.getRange(cols[i] + "6").setValue(null);
			calculator.getRange(cols[i] + "7").uncheck();
		} else {
			calculator.getRange(cols[i] + "6").setValue(teammembers[i]);
			if (solos[i] == 1) {
				calculator.getRange(cols[i] + "7").check();
			} else {
				calculator.getRange(cols[i] + "7").uncheck();
			}
		}
	}
	return true;
}

function copyRecommendedTeam() {
	copyTeam("F");
}

function copyPreviousTeam() {
	copyTeam("I");
}

function saveShadowlandRecord() {
	/* get Shadowland Calculator range */
	var spreadsheet = SpreadsheetApp.getActiveSpreadsheet();

	/* ensure data are valid based on current validators */
	var workingsheet = spreadsheet.getSheetByName("Shadowland Working Sheet");
	var validity = workingsheet.getRange("$S$2");
	if (validity.isBlank()) {
		ui = SpreadsheetApp.getUi();
		ui.alert(
			"Error",
			"Select an opposing team and a winning team.",
			ui.ButtonSet.OK
		);
		return false;
	} else if (!validity.getValue()) {
		ui = SpreadsheetApp.getUi();
		ui.alert(
			"Error",
			'Not a valid floor/opponent/team combination. If this is a new combination, check the "New Team" box.',
			ui.ButtonSet.OK
		);
		return false;
	}

	var calculator = spreadsheet.getSheetByName("Shadowland Calculator");
	var entry = calculator.getRange("$A$1:$K$9").getValues();

	var timestamp = new Date();
	var floorNum = entry[1][0];
	var floorMode = entry[4][0];
	var opponentTeam = entry[1][3];
	var winningTeam = entry[8][5].replace(/ \[Floor [^\]]*\]$/, "");
	var newentry = [
		timestamp,
		floorNum,
		floorMode,
		,
		,
		,
		opponentTeam,
		winningTeam,
	];

	spreadsheet.getSheetByName("Shadowland Record").appendRow(newentry);
	return true;
}

function resetShadowlandFloor() {
	// reset the information in the Shadowland calculator to redo the current floor
	var calculator = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(
		"Shadowland Calculator"
	);
	calculator
		.getRangeList(["$D$2", "$G$2", "$J$2", "$D$6", "$G$6", "$J$6"])
		.setValue(null);
	calculator
		.getRangeList(["$D$4", "$G$4", "$J$4", "$D$7", "$G$7", "$J$7", "$F$10"])
		.uncheck();
}

function nextShadowlandFloor() {
	// reset the information in the Shadowland calculator to advance to the next floor
	var calculator = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(
		"Shadowland Calculator"
	);
	var oldfloor = calculator.getRange("$A$2").getValue();
	if (oldfloor >= 30) {
		var ui = SpreadsheetApp.getUi();
		var response = ui.alert(
			"Congratulations!",
			"You have completed the first 30 floors of Shadowland. Reset the season now?",
			ui.ButtonSet.YES_NO
		);
		if (response == ui.Button.YES) {
			resetShadowlandSeason();
		} else {
			return true;
		}
	} else {
		resetShadowlandFloor();
		calculator.getRange("$A$2").setValue(oldfloor + 1);
		return true;
	}
}

function lastShadowlandFloor() {
	var calculator = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(
		"Shadowland Calculator"
	);
	var oldfloor = calculator.getRange("$A$2").getValue();
	if (oldfloor <= 1) {
		SpreadsheetApp.getUi().alert("You are on the first floor.");
		return true;
	} else {
		resetShadowlandFloor();
		calculator.getRange("$A$2").setValue(oldfloor - 1);
		return true;
	}
}

function recordAndAdvanceShadowland() {
	// copy the current (winning) information to the Shadowland Record and advance to the next floor
	if (saveShadowlandRecord()) {
		nextShadowlandFloor();
	}
}

function resetShadowlandSeason() {
	var calculator = SpreadsheetApp.getActiveSpreadsheet().getSheetByName(
		"Shadowland Calculator"
	);
	calculator.getRange("$A$2").setValue(1);
	resetShadowlandFloor();
}

/* checkForReset() evaluates the current time and whether any data resets are due
 * based on the last reset time, running the appropriate [time]Reset() function when
 * necessary.
 */
function checkForReset() {
	// see if a sheet is currently being worked on, if possible
	// maybe add an obvious but not modal alert to the sidebar, with an option to delay (updates a spreadsheet cell)
	// then check that cell when I start this to see if it's been delayed?
	// for the sidebar dialog, probably just modify the visibility of a hidden HTML div that warns about maintenance
	// but may have to open the sidebar if not already open, or open a dialog box if we can't open the sidebar
	// read appropriate times for reset from spreadsheet
	var file = DriveApp.getFileById("[spreadsheet ID not specified]");
	var spreadsheet = SpreadsheetApp.openById("[spreadsheet ID not specified]");
	var workingSheet = getSheetById(spreadsheet, 1507458603);

	// Unfortunately, a time-triggered script can't used getUi(), show(), etc. due to context

	// check to see if any of the "next reset time"s have passed since the last Reset
	var resetFrequencies = ["Daily", "Weekly", "Sunday"];
	var needsRun = workingSheet
		.getRange(2, 6, resetFrequencies.length)
		.getValues();

	var lastUpdate = file.getLastUpdated();
	// if last updated within ***, presume that it's open and give a warning or option to delay
	// would love to not do this and steal focus if a cell is currently being edited; or maybe that'll be okay?
}
