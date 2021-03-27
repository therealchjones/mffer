/***************************************************************
/  Marvel Future Fight Webapp - Freestanding Google Apps Script
/  Public domain - no rights reserved
***************************************************************/

/**
 * Returns the spreadsheet object for the Marvel Future Fight Google Sheet where
 * data are stored.
 */
function getSpreadsheet() {
  return SpreadsheetApp.openById( spreadsheetId );
}

/**
 * The basic webapp enabling function responding to the HTTP GET request
 *
 */
function doGet(e) {
  return HtmlService.createTemplateFromFile("Page.html")
    .evaluate()
    .addMetaTag('viewport', 'width=device-width, initial-scale=1, shrink-to-fit=no')
    .setTitle('Marvel Future Fight Manager');
}

/**
 * Allow inclusion of HTML from another file to allow
 * separate structure/style/script/etc.
 *
 */
function include( filename ) {
  return HtmlService.createHtmlOutputFromFile( filename ).getContent();
}

/**
 * Allow referring to an individual sheet by ID rather than name (which
 * may change).
 * modified from an excellent Stack Overflow answer at
 * https://stackoverflow.com/a/51789725
 * TODO: could put this into a library rather than in individual source when
 *       performance is not an issue
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
  var sheet = getSheetById( getSpreadsheet(),"1514300109" );
  var rows = sheet.getDataRange().getHeight();
  var range = sheet.getSheetValues(1,33,rows,32);
  return range[0].map( function( column ) {
    return range.map( function( row ) {
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
function saveNewPreferences( preferences ) {
  getSheetById( getSpreadsheet() , "1315797114" ).getRange(15,3,5).setValues( preferences );
}

/**
 * Saves a new row to the Shadowland record. The entry should be an array already
 * given in the appropriate order:
 * [ floor, mode, opponent team 1, opponent team 2, opponent team 3, selected opponent team,
 *   winning team ]
 */
function saveShadowlandEntry( entry ) {
  // As webapps aren't permitted to pass a time, we find it here
  entry.unshift( new Date() );
  getSheetById( getSpreadsheet() , "1930936724" ).appendRow( entry );
}

/**
 * Sets the "current floor" in the database
 * (i.e., the old sheet, see comments for saveNewPreferences())
 */
function saveFloorNumber( floorNumber ) {
  getSheetById( getSpreadsheet() , "1315797114" ).getRange( "A2" ).setValue( floorNumber );
}
