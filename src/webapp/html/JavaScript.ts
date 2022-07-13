"use strict";
var debug = true; // debug = true outputs more to the console
var homePage: JQuery | null = null;
var url: string | null = null;
document.addEventListener("DOMContentLoaded", start);
function start() {
	checkLocation();
	checkConfigured();
}
async function setConfigured() {
	await Promise.all([initializeStorage(), bootstrapify()]);
	homePage = $("#mffer-contents");
	initializePage();
}
function setNotConfigured() {
	homePage = $("#mffer-welcome");
	initializeSetup();
}
function checkLocation() {
	if (window.location.origin.includes(".googleusercontent.")) {
		google.script.run
			.withSuccessHandler(setUrl)
			.withFailureHandler((error, object) => throwError(error.toString()))
			.getUrl();
	}
}
function setUrl(serverUrl: string) {
	if (!url && serverUrl) url = serverUrl;
}
/** Various memory stores for volatile and persistent memory, complicated by
 * the inability to use window.localStorage directly for apps script since
 * it presents user code in an iframe from a different domain. Each is
 * scoped differently, with more narrow scopes overriding more broad ones:
 *
 * serverStorage: loaded from Apps Script server upon page load, sent back when
 *              changes are made, as persistent as possible
 * localStorage: script's window.localStorage, but due to third-party
 *               storage restrictions may persist only for the current session
 *               especially if the page is being loaded from Apps Script
 * workingStorage: volatile, in memory, resets with page load
 */
var workingStorage: { [key: string]: string | boolean | null } | null = null;
var serverStorage: { [key: string]: string | boolean | null } | null = null;
// var localStorage = null; // should already be defined if supported
async function initializeStorage() {
	if (workingStorage != null)
		throw new Error("Storage has already been initialized");
	workingStorage = {};
	let localStoragePromise = initializeLocalStorage();
	let serverStoragePromise = initializeServerStorage();

	const results = await Promise.allSettled([
		serverStoragePromise,
		localStoragePromise,
	]);
	if (results[0].toString() == "fulfilled" && serverStorage != null)
		workingStorage = { ...serverStorage };
	if (results[1].toString() == "fulfilled")
		workingStorage = { ...workingStorage, ...localStorage };
}
function initializeLocalStorage() {
	return new Promise<void>(function (resolve, _) {
		if (!window.localStorage)
			throw new Error("No local storage is available.");
		resolve();
	});
}
function initializeServerStorage() {
	return new Promise<void>(function (resolve, _) {
		if (serverStorage != null)
			throw new Error("server storage is already configured");
		google.script.run
			.withFailureHandler(() =>
				throwError("Unable to get server storage")
			)
			.withSuccessHandler(function (response: string) {
				let pageStorageText = response.trim();
				if (pageStorageText)
					serverStorage = JSON.parse(pageStorageText);
				else serverStorage = {};
				resolve();
			})
			.getConfig();
	});
}
function getMessageName(messageEvent: MessageEvent) {
	if (messageEvent == null || messageEvent.data == null)
		throw new Error("Invalid message received");
	else {
		if (messageEvent.data.name === undefined)
			return messageEvent.data.toString();
		else return messageEvent.data.name.toString();
	}
}
var initialListener: (messageEvent: MessageEvent<any>) => void;
function timeout(promise: Promise<any>, time: number) {
	return Promise.race([
		promise,
		new Promise<any>(function (_, reject) {
			setTimeout(reject, time);
		}),
	]);
}
// double array
var shadowlandDatabase: {
	[index: number]: {
		[index: number]: string | boolean | number | null;
	};
} | null = null;
var googlePicker = null;
var googleOauthToken: string = "";
var mfferSettings: { [index: string]: string | boolean } = {
	oauthId: "",
	oauthSecret: "",
	pickerApiKey: "",
};
var importText: string | null = null;
var bootstrapElements = {
	boxarrow:
		'<svg xmlns="http://www.w3.org/2000/svg" width="16" height="16" fill="currentColor" viewBox="0 0 16 16"><title>Open in new window</title><path fill-rule="evenodd" d="M6 3.5a.5.5 0 0 1 .5-.5h8a.5.5 0 0 1 .5.5v9a.5.5 0 0 1-.5.5h-8a.5.5 0 0 1-.5-.5v-2a.5.5 0 0 0-1 0v2A1.5 1.5 0 0 0 6.5 14h8a1.5 1.5 0 0 0 1.5-1.5v-9A1.5 1.5 0 0 0 14.5 2h-8A1.5 1.5 0 0 0 5 3.5v2a.5.5 0 0 0 1 0v-2z"/><path fill-rule="evenodd" d="M11.854 8.354a.5.5 0 0 0 0-.708l-3-3a.5.5 0 1 0-.708.708L10.293 7.5H1.5a.5.5 0 0 0 0 1h8.793l-2.147 2.146a.5.5 0 0 0 .708.708l3-3z"/></svg>',
	spinner:
		'<span class="spinner-border spinner-border-sm p-0" title="Loading..."></span>',
};

function checkConfigured() {
	google.script.run
		.withFailureHandler(function () {
			throwError("Unable to check for configuration");
		})
		.withSuccessHandler(function (response: boolean | string) {
			if (response == true) setConfigured();
			else setNotConfigured();
		})
		.isConfigured();
}
async function isConfigured() {
	return new Promise(function (resolve, reject) {
		google.script.run
			.withFailureHandler(() =>
				throwError("Unable to check for configuration")
			)
			.withSuccessHandler(function (response: string) {
				resolve(response);
			})
			.isConfigured();
	});
}

/**
 * Check for window.localStorage capabilities
 */
function hasLocalStorage() {
	if (!window.localStorage) return false;
	return true;
}
/**
 * Apply some standard Bootstrap classes rather than including them in all
 * the different HTML sources. Note that for best experience, the initially
 * displayed pages (such as .always-active) should not depend on this.
 */
async function bootstrapify() {
	let pages = $('body > [id^="mffer-"]').filter(':not(".always-active")');
	pages.addClass("container");
	pages.find("ul").addClass("list-group list-group-flush");
	pages.find("ol").addClass("list-group list-group-flush");
	pages.find("li").addClass("list-group-item");
	pages.find("label").addClass("form-label");
	pages.find("a[target='_blank']").append(bootstrapElements.boxarrow);
	bootstrapifyAdmin();
}
function bootstrapifyAdmin() {
	let lis = $("#mffer-admin li");
	let spinner = bootstrapElements.spinner;
	lis.append(spinner);
	lis.children(".spinner-border").hide();
	lis.find("input")
		.filter(':not([type="checkbox"])')
		.addClass("form-control");
	lis.find("input[type='text']")
		.addClass("pe-5")
		.each(function (index, elem) {
			let parent = $(elem).parent();
			let label = parent.children("label");
			if (
				label.length == 1 &&
				label.attr("for") == elem.id &&
				parent.children("input").length == 1
			) {
				parent.addClass("form-floating");
				label
					.remove()
					.appendTo(parent)
					.css({
						top:
							"calc( " +
							parent.css("padding-top") +
							" + " +
							label.siblings("p").css("height") +
							")",
						left: parent.css("padding-left"),
					});
				if (
					$(elem).attr("placeholder") == null ||
					$(elem).attr("placeholder") == ""
				)
					$(elem).attr("placeholder", "...");
			}
		});
}
function showLoading(message: string = "") {
	$("#loading-notes").text(message);
	hidePages();
	$("#mffer-spinner").show();
}
function hideLoading() {
	$("#mffer-spinner").hide();
	$("#loading-notes").text("");
}
function hidePages() {
	$("body")
		.children('[id^="mffer-"]')
		.filter(':not(".always-active")')
		.filter(':not("#mffer-alert")')
		.hide();
}
function receiveFrameStorageMessage(messageEvent: MessageEvent) {
	let messageName = getMessageName(messageEvent);
	switch (messageName) {
		case "frameStorageRetrieve":
			if (
				messageEvent.data.key === undefined ||
				messageEvent.data.value === undefined
			)
				debugLog("Frame storage 'retrieve' message has no data");
			if (workingStorage == null) workingStorage = {};
			workingStorage[messageEvent.data.key] = messageEvent.data.value;
			debugLog(
				"Set mfferStorage." +
					messageEvent.data.key +
					" to '" +
					messageEvent.data.value +
					"'"
			);
			break;
		case "frameStorageStore":
			break;
		default:
			debugLog("Received unknown message type: '" + messageName + "'");
			break;
	}
	return;
}
function debugLog(message?: string) {
	if (debug) console.log(message);
}
function showAlert(messageOrTitle: string, message: string | null = null) {
	let title = "Alert";
	if (message == null) {
		message = messageOrTitle;
	} else {
		title = messageOrTitle;
	}
	$("#mffer-alert-title").html(title);
	$("#mffer-alert-text").html(message);
	$("#mffer-alert").show();
}
function showSpinner(element: HTMLElement | JQuery<Element>) {
	getSpinner(element).show();
}
function hideSpinner(element: HTMLElement | JQuery<Element>) {
	getSpinner(element).hide();
}
function getSpinner(element: HTMLElement | JQuery<Element>) {
	let spinner = $(element).siblings(".spinner-border");
	if (spinner.length == 0 && $(element).parent("li").length == 0) {
		spinner = $(element).parent().siblings(".spinner-border");
	}
	return spinner;
}
function initializeSetup() {
	$("#mffer-new").show();
	bootstrapify();
	enableAdminContent();
	$("#mffer-new-button-setup")
		.on("click", openAdmin)
		.html("setup mffer")
		.removeAttr("disabled");
}
function openContents() {
	hidePages();
	if (homePage != null) homePage.show();
}
function openAdmin() {
	hidePages();
	let lis = $("#mffer-admin li");
	for (let property in mfferSettings) {
		let input = $("#mffer-admin-input-" + property.toLowerCase());
		if (input.length != 0) {
			showSpinner(input);
		}
	}
	showSpinner($("#mffer-admin-input-oauthredirecturi"));
	google.script.run
		.withSuccessHandler(function (uri: string) {
			$("#mffer-admin-input-oauthredirecturi").val(uri);
			hideSpinner($("#mffer-admin-input-oauthredirecturi"));
		})
		.withFailureHandler(function () {
			throwError("Unable to get redirect URI");
		})
		.getRedirectUri();
	$("#mffer-admin")
		.find("input[type='text']")
		.on("change", function (event) {
			showSpinner(event.currentTarget);
			let text = $(event.currentTarget).val()?.toString().trim();
			if (text == null) text = "";
			let inputName = event.currentTarget.id;
			let shortName = inputName.replace(/^mffer-admin-input-/, "");
			for (let property of Object.keys(mfferSettings)) {
				if (property.toLowerCase() == shortName) {
					mfferSettings[property] = text;
				}
			}
			switch (shortName) {
				case "oauthid":
					let link =
						"https://console.cloud.google.com/apis/credentials";
					let html = "GCP credentials";
					if (!isFalseOrEmpty(text)) {
						link =
							"https://console.cloud.google.com/apis/credentials/oauthclient/" +
							mfferSettings.oauthId;
						html = "OAuth ID settings";
					}
					$("#mffer-admin-instructions-oauthsecret")
						.add("#mffer-admin-instructions-oauthredirecturi")
						.find("a")
						.attr("href", link)
						.html(html)
						.append(bootstrapElements.boxarrow);
					break;
				default:
					break;
			}
			hideSpinner(event.currentTarget);
		});
	google.script.run
		.withSuccessHandler(loadSettings)
		.withFailureHandler(function () {
			throwError("Unable to get the current configuration");
		})
		.getConfig();
	$("#mffer-filechooser-pending button").click(function (event) {
		$("#mffer-filechooser-pending").hide();
		switch (event.target.id) {
			case "button-filechooser-confirm":
				showSpinner($("#mffer-admin-input-fileupload"));
				google.script.run
					.withSuccessHandler(uploadComplete)
					.withFailureHandler(function () {
						throwError("Unable to import new data");
					})
					.importNewData(importText, JSON.stringify(workingStorage));
				break;
			case "button-filechooser-cancel":
				$("#mffer-filechooser-pending").hide();
				$("#button-filechooser-confirm").attr("disabled", "disabled");
				$("#mffer-admin-input-fileupload").val("");
				$("#mffer-admin-input-fileupload").change();
				$("#mffer-admin").show();
				break;
			default:
				throwError("Unable to identify button clicked");
		}
	});
	let fileUploader: JQuery<HTMLInputElement> = $(
		"#mffer-admin-input-fileupload"
	);
	fileUploader.change(function () {
		if (this.files != null && this.files.length != 0) {
			if (this.files.length > 1) {
				throwError("Selection of multiple files is not supported.");
			}
			$("#mffer-filechooser-pending").show();
			let file = this.files[0];
			let fileReader = new FileReader();
			fileReader.onload = function () {
				let readText = fileReader.result?.toString();
				if (readText === undefined) importText = "";
				else importText = readText;
				let headers = getFirstLines(importText);
				$("#mffer-filechooser-textdisplay").html(
					"<pre>\n" + headers + "\n</pre>"
				);
				$("#mffer-filechooser-tabledisplay").html(csvToTable(headers));
			};
			fileReader.onerror = function () {
				throwError("Unable to read file");
			};
			fileReader.readAsText(file);
		}
	});
	$("#mffer-admin-button-authorize").click(adminAuthAndSave);
	$("#mffer-admin").show();
}
function isFalseOrEmpty(check: any) {
	if (
		!check ||
		check.toString().trim() === "" ||
		JSON.stringify(check) == "{}"
	)
		return true;
	return false;
}
async function adminAuthAndSave() {
	if (!(await isConfigured())) {
		if (
			isFalseOrEmpty(mfferSettings.oauthId) ||
			isFalseOrEmpty(mfferSettings.oauthSecret)
		) {
			$("#mffer-admin-authorizationerror").html(
				"OAuth ID and OAuth secret are required"
			);
		}
	}
	$("#mffer-admin-authorizationerror").html("");
	$("#mffer-admin-button-authorize")
		.off("click")
		.attr("disabled", "true")
		.html(bootstrapElements.spinner);
	let properties: { [index: string]: string | boolean } = {};
	for (let property of Object.keys(mfferSettings)) {
		if (
			!$("#mffer-admin-input-" + property.toLowerCase()).attr("disabled")
		) {
			properties[property] = mfferSettings[property];
		}
	}
	let propertiesJson = JSON.stringify(properties);
	google.script.run
		.withFailureHandler(function (error: any) {
			error = error.toString();
			throwError("Unable to obtain admin login URL: " + error);
		})
		.withSuccessHandler(function (url: any) {
			url = url.toString();
			if (isFalseOrEmpty(url))
				throw new Error(
					"Unable to obtain admin login url: empty result"
				);
			console.log(`Authorization URL: ${url}`);
			$(document.createElement("a")).attr("href", url)[0].click();
		})
		.getAdminAuthUrl(propertiesJson);
}
function throwError(error: string) {
	$("#mffer-spinner h2").html("Error");
	let errorMessage =
		"<p>" +
		error +
		"</p><p>More information may be available in your browser's JavaScript console or in Google Scripts execution logs.</p>";
	$("#loading-notes").html(errorMessage);
	throw error;
}
function loadSettings(settingString: string = "{}") {
	let currentSettings: { [index: string]: string | boolean | null } =
		JSON.parse(settingString);
	for (let property in currentSettings) {
		let value = currentSettings[property];
		if (value === null) value = "";
		mfferSettings[property] = value;
	}
	for (let property in mfferSettings) {
		let input = $("#mffer-admin-input-" + property.toLowerCase());
		if (input.length != 0) {
			let text: string = "";
			if (!isFalseOrEmpty(mfferSettings[property])) {
				text = mfferSettings[property].toString();
				if (mfferSettings[property] === true) text = "[set]";
			}
			if (isFalseOrEmpty(input.val())) input.val(text);
			else {
				let value = input.val()?.toString();
				value
					? (mfferSettings[property] = value)
					: (mfferSettings[property] = "");
			}
			hideSpinner(input);
			input.change();
		}
	}
}
function loadPickerApi(token: any) {
	if (token == null) throwError("Unable to obtain authorization.");
	googleOauthToken = token.toString();
	showLoading("Loading Google Picker");
	$.getScript("https://apis.google.com/js/api.js")
		.done(function () {
			gapi.load("picker", accessGooglePicker);
		})
		.fail(function () {
			throw "Unable to load Google API";
		});
}
function accessGooglePicker() {
	google.script.run
		.withSuccessHandler(createGooglePicker)
		.withFailureHandler(function () {
			throwError("Unable to obtain Google Picker API Key");
		})
		.getPickerApiKey();
}
function createGooglePicker(apiKey?: string) {
	let pickerApiKey: string = "";
	if (apiKey != null) {
		pickerApiKey = apiKey;
	}
	let spreadsheetView = new google.picker.DocsView(
		google.picker.ViewId.SPREADSHEETS
	);
	spreadsheetView
		.setIncludeFolders(true)
		.setMode(google.picker.DocsViewMode.LIST);
	googlePicker = new google.picker.PickerBuilder()
		.addView(spreadsheetView)
		.hideTitleBar()
		.setOAuthToken(googleOauthToken)
		.setDeveloperKey(pickerApiKey)
		.setOrigin(google.script.host.origin)
		.setCallback(pickerCallback)
		.build();
	hideLoading();
	googlePicker.setVisible(true);
}
function pickerCallback(data: any) {
	if (data[google.picker.Response.ACTION] == google.picker.Action.PICKED) {
		if (
			data[google.picker.Response.DOCUMENTS] != null &&
			data[google.picker.Response.DOCUMENTS][0] != null
		) {
			$("#mffer-new-get-database").hide();
			showLoading("Checking file");
			google.script.run
				.withSuccessHandler(() => null)
				.withFailureHandler(function () {
					throwError("Unable to use this file.");
				});
		}
	}
}
function uploadComplete() {
	hideSpinner($("#mffer-admin-input-fileupload"));
	initializePage();
}
function showLoginSpinner() {
	$("#mffer-navbar-button-login").html(bootstrapElements.spinner).show();
}
function checkLocalStorage() {
	if (!workingStorage) workingStorage = {};
	if (hasLocalStorage()) {
		if (window.localStorage.getItem("oauth2.userLogin"))
			workingStorage["oauth2.userLogin"] =
				window.localStorage.getItem("oauth2.userLogin");
	}
}
function checkUserLogin() {
	showLoginSpinner();
	checkPageStorage();
	if (!workingStorage || !workingStorage["oauth2.userLogin"]) {
		openContents();
		// in-page storage did not include new user data; check for
		// persistent old user data
		checkLocalStorage();
		if (!workingStorage || !workingStorage["oauth2.userLogin"]) {
			// there's no user data at all
			resetLogin();
		} else {
			// there was persistent old user data, but it needs to be
			// updated and/or checked
			updateLogin(JSON.stringify(workingStorage));
		}
	}
}
function checkPageStorage() {
	if (serverStorage && serverStorage.adminAuthError) {
		// failed admin login when trying to change settings
		// TODO: #147 consider whether admin auth error should return
		// attempted auth settings
		userLogout();
		showAlert("Administrator authentication error");
		openContents();
	} else if (serverStorage && serverStorage["oauth2.adminLogin"]) {
		// logged in to change settings; will go back to admin page but
		// also login as "regular" user
		if (!workingStorage) workingStorage = {};
		workingStorage["oauth2.userLogin"] = serverStorage["oauth2.adminLogin"];
		updateLogin(JSON.stringify(workingStorage));
		enableAdminContent();
		openAdmin();
	} else if (serverStorage && serverStorage.userAuthError) {
		// failed standard user login
		userLogout();
		showAlert("User authentication error");
		openContents();
	} else if (serverStorage && serverStorage["oauth2.userLogin"]) {
		// logged in as standard user
		setLoginStatus(JSON.stringify(serverStorage));
		openContents();
	}
}
function updateLogin(storageText: string) {
	google.script.run
		.withSuccessHandler(setLoginStatus)
		.withFailureHandler(function () {
			throwError("Unable to get login status");
		})
		.getUserLoginStatus(storageText);
}
function savePageStorage() {
	if (hasLocalStorage()) {
		if (
			workingStorage != null &&
			workingStorage["oauth2.userLogin"] != null
		)
			window.localStorage.setItem(
				"oauth2.userLogin",
				workingStorage["oauth2.userLogin"].toString()
			);
	}
}
function resetLogin() {
	$("#mffer-navbar-button-login").html("login").click(userLogin).show();
}
function setLoginStatus(storageText: string | null) {
	if (!storageText) {
		// user does not (currently) have access
		if (hasLocalStorage()) localStorage.clear();
		resetLogin();
	} else {
		let newStorage = JSON.parse(storageText);
		workingStorage = {};
		workingStorage["oauth2.userLogin"] = newStorage["oauth2.userLogin"];
		if (newStorage.adminUser) {
			workingStorage.adminUser = true;
			enableAdminContent();
		} else hideAdminContent();
		if (newStorage.hasUserSpreadsheet)
			workingStorage.hasUserSpreadsheet = true;
		else
			showAlert(
				"Spreadsheet not found",
				"Please select or create a Google Spreadsheet to save your individual data"
			);
		savePageStorage();
		$("#mffer-navbar-button-login").html("logout").click(userLogout).show();
	}
}
function enableAdminContent() {
	$("#mffer-contents-li-admin")
		.add("#menu-item-admin")
		.removeClass("start-inactive");
}
function hideAdminContent() {
	if ($("#mffer-admin").is(":visible")) {
		openContents();
	}
	$("#mffer-contents-li-admin").add("#menu-item-admin").hide();
}
function userLogin() {
	showLoginSpinner();
	savePageStorage();
	google.script.run
		.withFailureHandler(function () {
			throwError("Unable to get user login URL");
		})
		.withSuccessHandler(function (url: any) {
			$(document.createElement("a"))
				.attr("href", url.toString())[0]
				.click();
		})
		.getUserAuthUrl(workingStorage);
}
function userLogout() {
	hideAdminContent();
	workingStorage = null;
	serverStorage = null;
	$("#mffer-storage").html("");
	setLoginStatus(null);
}
function initializePage() {
	showLoading("getting data");
	google.script.run
		.withFailureHandler(alertLoadFailure)
		.withSuccessHandler(loadData)
		.getWebappDatabase();
	$("#mffer-admin-input-oauthid")
		.add("#mffer-admin-input-oauthsecret")
		.attr("disabled", "disabled");
	checkUserLogin();
}
function loadData(database: any) {
	if (database == null) alertLoadFailure();
	hideLoading();
}

// reset the page settings and info for a given floor number, on the current
// floor (if set to null or undefined), or from the database (if set
// to anything that isNaN)
function resetPage(floorNumber: any) {
	// we build and set the appropriate fields in turn

	// floor number
	var currentFloor = 0;
	if (floorNumber == null) {
		currentFloor = getCurrentFloor();
	} else if (isNaN(floorNumber)) {
		currentFloor = getCurrentFloor("database");
	} else {
		currentFloor = floorNumber;
	}
}

// returns the current floor number, determining it based on the
// current page information if present, from the database if not or
// if any NaN is given as the argument; this is *not* the same as
// returning the number of the first uncompleted floor as previously
// may have happened. If floorNumber is given and is a number,
// throws an exception, as that's probably meant to be an attempt
// to *set* the current floor number rather than obtain it. Finally,
// in case we're not obtaining the number from the database, set the
// sheet calculator to it for the next time we pull the database.
function getCurrentFloor(floorNumber?: any): number {
	let currentFloor = 0;
	if (floorNumber == null) {
		// determine the floor number from the page
		// if unable to find, set to NaN
	} else if (isNaN(floorNumber)) {
		if (shadowlandDatabase == null) {
			throwError("Unable to read database");
			return 0;
		}
		try {
			currentFloor = Number(shadowlandDatabase[28][1]);
		} catch (err) {
			currentFloor = 0;
		}
	} else {
		alertHalt("Improper use of getCurrentFloor( " + floorNumber + " )");
	}

	if (currentFloor < 1) {
		alertError(
			"Unable to determine current floor. Consider setting manually."
		);
		return 0;
	} else {
		saveFloorNumber(currentFloor);
		return currentFloor;
	}
}

function markProgress(text: string, jobNumber = null) {
	if (jobNumber == null) {
		// make job number
		// write text to log & progress bar
	} else {
		// try to find job based on job number
		// write text to log & progress bar
	}
	// return job number
}

// for issues that may not be right, but don't necessarily require user intervention
function alertWarn(text: string) {
	alertError(text);
}

// for issues that require user intervention
function alertError(text: string) {
	$("#mffer-alert").html(text);
	$("#mffer-alert").show();
}

// for issues that are unrecoverable
function alertHalt(text: string | null) {
	window.alert(text);
	throw new Error("Unrecoverable: " + text);
}

function alertLoadFailure(error: Error | null = null) {
	if (error == null) {
		alertError("Unable to load the database.");
	} else {
		alertError(
			"Error loading the database: " + error.name + ": " + error.message
		);
	}
	hideLoading();
}
function alertSaveFailure(error: Error, userObj?: any) {
	alertError(
		"Error saving the battle record: " + error.name + ": " + error.message
	);
}

function getJobText(jobNumber: number) {
	// get the text associated with this jobNumber from the message queue
	return jobNumber;
}

function saveFloorNumber(floorNumber: number) {
	let jobNumber = markProgress("Saving current floor");
	google.script.run
		.withFailureHandler(logWarn)
		.withSuccessHandler(logSuccess)
		.withUserObject(jobNumber)
		.saveFloorNumber(floorNumber);
	return floorNumber;
}
function logSuccess(result: any): void {}
function logWarn(error: Error) {}
function getFirstLines(text: string) {
	var maxDisplayLines = 3;
	var maxCharsPerLine = 140;
	var maxCharsTotal = 420;
	var i = 0;
	var startIndex = 0;
	var endIndex = 0;
	var returnText = "";
	var totalChars = text.length;
	while (i < maxDisplayLines) {
		i++;
		var nextNewLine = text.indexOf("\n", startIndex + 1);
		if (nextNewLine == -1) {
			endIndex = Math.min(startIndex + maxCharsPerLine, totalChars);
			returnText += text.substring(startIndex, endIndex);
			break;
		} else {
			endIndex = Math.min(startIndex + maxCharsPerLine, nextNewLine);
			returnText += text.substring(startIndex, endIndex);
			startIndex = nextNewLine;
		}
	}
	if (returnText.length > maxCharsTotal) {
		returnText = returnText.substring(0, maxCharsTotal);
	}
	return returnText;
}
function csvToTable(text: string) {
	google.script.run
		.withSuccessHandler(displayTable)
		.withFailureHandler(displayTableError)
		.csvToTable(text, JSON.stringify(workingStorage));
	return "<p>" + bootstrapElements.spinner + "</p>";
}
function displayTable(text: string) {
	let result = $("<div>" + text + "</div>");
	let storage: { [key: string]: string } = {};
	if (result.find("#mffer-admin-filechooser-pending-storage").length > 0) {
		storage = JSON.parse(
			result
				.find("#mffer-admin-filechooser-pending-storage")
				.html()
				.trim()
		);
		if (storage.adminAuthError) {
			result.append(
				'<div id="mffer-admin-filechooser-pending-warning" class="text-danger">' +
					"Warning: no administrator is logged in. Data will not be imported. Login as administrator first, then try again." +
					"</div>"
			);
		}
	}
	$("#button-filechooser-confirm").removeAttr("disabled");
	$("#mffer-filechooser-tabledisplay").html(result.html());
}
function displayTableError(error: Error) {
	$("#mffer-filechooser-tabledisplay").html(
		"<p>Parsing failed:</p>\n<p>" + error.message + "</p>"
	);
}
