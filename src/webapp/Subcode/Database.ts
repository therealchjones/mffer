class Database {
	readonly Spreadsheet: GoogleAppsScript.Spreadsheet.Spreadsheet;
	readonly id: string;
	readonly sheetNames: string[] = ["Main", "mffer"];
	readonly databaseFile: GoogleAppsScript.Drive.File;
	constructor(
		spreadsheet: GoogleAppsScript.Spreadsheet.Spreadsheet = null,
		database: GoogleAppsScript.Drive.File = null
	) {
		if (database == null) {
			this.databaseFile = null;
		} else {
			this.databaseFile = database;
		}
		if (spreadsheet == null) {
			this.Spreadsheet = SpreadsheetApp.create("mffer Database");
		} else {
			this.Spreadsheet = spreadsheet;
		}
		this.makeSheets();
		this.id = this.Spreadsheet.getId();
	}
	/**
	 * makeSheets creates the required sheets within the Spreadsheet if they
	 * don't exist, and does some limited validation if they do
	 */
	public makeSheets(): void {
		for (let sheetIndex in this.sheetNames) {
			let sheet = this.Spreadsheet.getSheetByName(
				this.sheetNames[sheetIndex]
			);
			if (sheet == null) {
				sheet = this.Spreadsheet.insertSheet(
					this.sheetNames[sheetIndex],
					0
				);
				this.makeSheet(this.sheetNames[sheetIndex]);
			}
			this.Spreadsheet.setActiveSheet(sheet);
			this.Spreadsheet.moveActiveSheet(Number(sheetIndex));
		}
	}
	public getId(): void {
		this.Spreadsheet.getId();
	}
	/**
	 * makeSheet
	 */
	public makeSheet(sheetName: string): void {
		let sheet: GoogleAppsScript.Spreadsheet.Sheet;
		switch (sheetName) {
			case "Main":
				sheet = this.Spreadsheet.getSheetByName(sheetName);
				if (sheet.getDataRange().isBlank()) {
					this.makeMainSheet(sheet);
				}
				break;
			case "mffer":
				sheet = this.Spreadsheet.getSheetByName(sheetName);
				this.makeDataSheet(sheet);
				break;
			default:
				this.Spreadsheet.getSheetByName(sheetName).hideSheet();
				break;
		}
	}
	public makeDataSheet(dataSheet: GoogleAppsScript.Spreadsheet.Sheet): void {
		if (!dataSheet.getDataRange().isBlank()) {
			dataSheet.clear();
		}
		dataSheet.hideSheet();
	}
	/**
	 * makeMainSheet
	 */
	public makeMainSheet(mainSheet: GoogleAppsScript.Spreadsheet.Sheet) {
		mainSheet.deleteColumns(2, mainSheet.getMaxColumns() - 1);
		mainSheet.deleteRows(2, mainSheet.getMaxRows() - 1);
		let linkText = "the mffer development guide.";
		let linkBuilder = SpreadsheetApp.newRichTextValue().setText(linkText);
		linkBuilder.setLinkUrl(
			0,
			linkText.length - 2,
			"https://github.com/therealchjones/mffer/docs/Development.md"
		);
		let link = linkBuilder.build();
		let mainText = [
			["Warning: this spreadsheet is not intended to be edited by hand."],
			[
				"This spreadsheet includes data specifically formatted for the mffer webapp.",
			],
			["Any changes may cause the app to malfunction."],
			["Updating the data via the webapp may overwrite any changes."],
			["Before making any changes to this spreadsheet, see"],
			[link],
		];
		let mainStyle = SpreadsheetApp.newTextStyle()
			.setBold(true)
			.setFontSize(20)
			.setForegroundColor("white")
			.build();
		let range = mainSheet.getRange(1, 1, mainText.length, 1);
		range
			.setValues(mainText)
			.setTextStyle(mainStyle)
			.setBackground("black")
			.setWrap(true);
		mainSheet.autoResizeColumn(1);
	}
}
