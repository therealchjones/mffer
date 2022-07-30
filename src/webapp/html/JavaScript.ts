"use strict";

/**
 * enables additional testing, though no additional functionality; adds output
 * in the browser's JavaScript console
 */
var mfferDebug: boolean = true;
/** deploymentId is set to a nonempty string iff the page is being served from
 * a domain other than Google Apps Script.
 */
// (for use in the page building process, don't change the below line)
var deploymentId: string = "";
/**
 * the "main" page displayed by default
 */
var homePage: JQuery | null = null;
/**
 * current page settings
 */
var settings: {
	oauthId: string | null;
	hasOauthSecret: boolean | null;
	"oauth2.userLogin": string | null;
	user: string | null;
	isAdmin: boolean | null;
	hasUserSpreadsheet: boolean | null;
	pickerApiKey: string | null;
} | null = null;

/**
 * `systemData` indexes Promises for data that is slow or complicated to access;
 * the Promises should be used only for reading the data, not for changing it.
 */
var systemData: { [key: string]: Promise<any> } = {};

document.addEventListener("DOMContentLoaded", start);
async function start() {
	loadSystemData("configuration", loadConfiguration);
	loadSystemData("url", loadUrl);
	loadSystemData("parameters", loadParameters);
	loadSystemData("credentials", loadCredentials);
	loadSystemData("texts", loadTexts);
	loadSystemData("loginurl", loadUserLoginUrl);
	loadSystemData("initialsettings", loadInitialSettings);
	initializeSettings();
	if (await isConfigured()) initializePage();
	else initializeSetup();
}
/**
 * reads data into a Promise that can be accessed multiple times to access the data without repetitive or overlapping calls
 * @param name key under which to store the Promise in the `systemData` object
 * @param func function which loads the data and returns the Promise
 */
async function loadSystemData(name: string, func: () => Promise<any>) {
	if (!systemData) systemData = {};
	if (!systemData[name]) systemData[name] = func();
}
/**
 * Overwrites the existing Promise associated with `name` with another. This is
 * not as safe or efficient as using `loadSystemData`.
 * @param name key under which to store the Promise in the `systemData` object
 * @param func function which loads the data and returns the Promise
 */
async function reloadSystemData(name: string, func: () => Promise<any>) {
	if (!systemData) systemData = {};
	systemData[name] = func();
}
/**
 * accesses a Promise for a copy of system data to avoid accessing the data directly with repetitive or overlapping calls
 * @param name key under which the Promise is stored in the `systemData` object
 * @returns a Promise for a copy of the data; not suitable for changing the data directly, just reading it
 */
async function getSystemData(name: string): Promise<any> {
	if (!systemData || !systemData[name])
		throw new Error(`System '${name}' has not been loaded`);
	return systemData[name];
}
function logDebug(message: string) {
	if (mfferDebug) console.debug(message);
}
/**
 * parses the HTML source to set default text based on the original text
 * within various elements
 */
async function loadTexts(): Promise<{ [key: string]: string }> {
	return new Promise((resolve, _) => {
		let defaultAdminSubmitText =
			document.getElementById("mffer-admin-button-authorize")
				?.innerHTML || "";
		resolve({
			defaultAdminSubmitText: defaultAdminSubmitText,
		});
	});
}
/**
 * creates the global `settings` object and loads it with the initial page settings
 * @returns a Promise to complete loading the `settings` object
 */
async function initializeSettings() {
	loadSystemData("initialsettings", loadInitialSettings);
	if (settings) {
		console.warn(
			"Settings have already been initialized; not doing it again."
		);
		return settings;
	}
	let initialSettings: any = await getSystemData("initialsettings");
	settings = {
		oauthId: initialSettings["oauthId"]?.toString() || null,
		hasOauthSecret: initialSettings["hasOauthSecret"],
		"oauth2.userLogin":
			initialSettings["oauth2.userLogin"]?.toString() || null,
		user: initialSettings["user"]?.toString() || null,
		isAdmin: initialSettings["isAdmin"],
		hasUserSpreadsheet: initialSettings["hasUserSpreadsheet"],
		pickerApiKey: initialSettings["pickerApiKey"]?.toString() || null,
	};
	return settings;
}
/**
 * checks whether this page is being served from Google Apps Script
 * @returns `true` if the page is being served from Google Apps Script, `false` otherwise
 */
function isOnGas(): boolean {
	return window.location.hostname.endsWith(".googleusercontent.com");
}
async function postRequest(functionName: string, ...args: any[]): Promise<any> {
	let postUrl: string;
	if (deploymentId)
		postUrl =
			"https://script.google.com/macros/s/" + deploymentId + "/exec";
	else {
		loadSystemData("url", loadUrl);
		postUrl = await getSystemData("url");
	}
	if (!functionName) {
		throw new Error("Unable to post request: no function name given");
	}
	let request: {
		application: string;
		function: { name: string; args: string[] };
	} = {
		application: "mffer",
		function: {
			name: functionName,
			args: [],
		},
	};
	args.forEach(function (value, index, _) {
		request.function.args[index] = JSON.stringify(value);
	});
	let requestJson = JSON.stringify(request);
	// Google Apps Script errors don't have Access-Control-Allow-Origin headers,
	// so no data will be returned; additionally, Google Apps Script does not
	// respond appropriately to CORS preflight, so our request must only use
	// items that do not trigger CORS preflight; see
	// https://developer.mozilla.org/en-US/docs/Glossary/Preflight_request and
	// https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS#simple_requests.
	// Specifically for our purposes, the 'Content-type' header is limited.
	let options: RequestInit = {
		method: "POST",
		mode: "cors",
		body: requestJson,
	};
	return fetch(postUrl, options).then(parseResponse, throwFetchError);
}
async function parseResponse(response: Response): Promise<any> {
	if (!response.ok)
		throw new Error(
			"Could not obtain requested data: " + JSON.stringify(response)
		);
	else {
		try {
			let respObj = response.json();
			return respObj;
		} catch (e: any) {
			throwError(
				"Unable to parse server response as JSON: " +
					response.text() +
					"(" +
					e.toString() +
					")"
			);
		}
	}
}
function throwFetchError(reason: any): never {
	throw new Error("Unable to fetch data: " + reason?.toString());
}
/**
 * loads the "parameters" system
 * @returns a Promise for the `parameters` object, a string-indexed dictionary
 * of strings
 */
async function loadParameters(): Promise<{ [key: string]: string | null }> {
	return new Promise<{ [key: string]: string | null }>(async function (
		resolve,
		_
	) {
		logDebug("Checking URL parameters");
		let parameters: { [key: string]: string | null } = {};
		if (isOnGas()) {
			parameters = await getGasParameters();
		} else {
			if (!systemData.url) systemData["url"] = loadUrl();
			let urlObj = new URL(await systemData.url);
			for (const key of urlObj.searchParams.keys()) {
				parameters[key] = urlObj.searchParams.get(key);
			}
		}
		resolve(parameters);
	});
}

/**
 * obtains credentials from the Apps Script server if the page is responding to
 * a callback
 * @returns a Promise for the server's `credentials` object whose format depends
 * on the specific callback that was invoked; if the page is not a callback
 * (i.e., the loaded url has no query string with a "state" parameter), the
 * empty object `{}` is returned
 */
async function loadCredentials(): Promise<{ [key: string]: string }> {
	return new Promise(async (resolve, _) => {
		loadSystemData("parameters", loadParameters);
		let params = await getSystemData("parameters");
		let credentials: any = {};
		if (Object.keys(params).includes("state")) {
			logDebug("Sending callback parameters to Apps Script");
			// some sort of callback (or reload)
			// if already logged in, check to see if it's the same login or a state that's already been used?
			// determine whether we're looking for a user or admin login?
			let response: { parameter: { [key: string]: string | null } } = {
				parameter: params,
			};
			// send to server
			await postRequest("processParameters", response)
				.then(function (creds: any) {
					if (creds) credentials = creds;
				})
				.catch((error: any) => {
					console.error(
						"Unable to process parameters: " + error?.toString()
					);
				});
		}
		resolve(credentials);
	});
}
/**
 * Determines the top address that returned this page. If the app is running on
 * Google Apps Script, this string is the origin and path; if not on Google Apps
 * script, it is the full href.
 * @returns a Promise for the url as a string
 */
async function loadUrl() {
	return new Promise<string>(function (resolve, _) {
		logDebug("Checking URL");
		if (isOnGas()) {
			google.script.run
				.withSuccessHandler(async function (gasUrl: any) {
					if (!gasUrl)
						throw new Error(
							"Unable to obtain URL from Apps Script"
						);
					resolve(gasUrl.toString());
				})
				.withFailureHandler((error) =>
					throwError(
						"Unable to get URL from Apps Script: " +
							error.toString()
					)
				)
				.loadUrl();
		} else {
			if (!window.location.href)
				throw new Error("Unable to determine url from window.location");
			resolve(window.location.href);
		}
	});
}
async function getGasParameters(): Promise<{ [key: string]: string }> {
	return new Promise(function (resolve, _) {
		logDebug("Getting parameters from Apps Script");
		if (!isOnGas())
			throw new Error(
				"Unable to obtain parameters using getGasParameters when not serving from Apps Script"
			);
		google.script.url.getLocation((location) =>
			resolve(location.parameter)
		);
	});
}
/**
 * read storage sources and consolidate the settings existing at page load time
 * @returns a Promise for the initial settings
 */
async function loadInitialSettings(): Promise<any> {
	return new Promise(async (resolve, _) => {
		loadSystemData("configuration", loadConfiguration);
		let storage: { [key: string]: string | null } = {
			...(await getSystemData("configuration")),
		};
		if (!window.localStorage) console.warn("No local storage is available");
		else storage = { ...storage, ...window.localStorage };
		resolve(storage);
	});
}
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
async function isConfigured(): Promise<boolean> {
	return new Promise<boolean>(async (resolve, _) => {
		loadSystemData("configuration", loadConfiguration);
		let config = await getSystemData("configuration");
		if (config && config.oauthId && config.hasOauthSecret) resolve(true);
		else resolve(false);
	});
}
/**
 * loads configuration from the Apps Script server
 * @returns a Promise for a configuration object, which is the empty object `{}` if the deployment is not configured
 */
async function loadConfiguration(): Promise<{
	[key: string]: string | boolean | null;
}> {
	return new Promise(async (resolve, _) => {
		// if there's a callback, we don't want to get the configuration until
		// that's been processed, which is done via credentialling
		loadSystemData("credentials", loadCredentials);
		await getSystemData("credentials");
		logDebug("Getting deployment configuration");
		postRequest("getConfig")
			.then((config: any) => {
				if (!config) resolve({});
				else {
					try {
						let configObj: any = JSON.parse(config);
						configObj = standardizeObject(configObj);
						resolve(configObj);
					} catch (e: any) {
						throwError(
							"Unable to parse configuration: " +
								JSON.stringify(config) +
								"(" +
								e.toString() +
								")"
						);
					}
				}
			})
			.catch(function (reason?: any) {
				throwError(
					"Unable to get the current configuration: " +
						reason?.toString()
				);
			});
	});
}
function standardizeObject(obj: any): {
	[key: string]: string | boolean | null;
} {
	let newObj: { [key: string]: string | boolean | null } = {};
	if (typeof obj != "object")
		throw new Error("Unable to standardize object " + JSON.stringify(obj));
	else {
		for (const key of Object.keys(obj)) {
			if (obj[key] == null) newObj[key] = null;
			else if (typeof obj[key] == "boolean") newObj[key] = obj[key];
			else if (typeof obj[key] == "string") newObj[key] = obj[key];
			else newObj[key] = obj[key].toString();
		}
		return newObj;
	}
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
	logDebug("Starting webapp configuration");
	homePage = $("#mffer-new");
	homePage.show();
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
	logDebug("Switching to admin configuration page");
	hidePages();
	let lis = $("#mffer-admin li");
	for (let property in mfferSettings) {
		let input = $("#mffer-admin-input-" + property.toLowerCase());
		if (input.length != 0) {
			showSpinner(input);
		}
	}
	showSpinner($("#mffer-admin-input-oauthredirecturi"));
	postRequest("getRedirectUri")
		.then(function (uri: string) {
			$("#mffer-admin-input-oauthredirecturi").val(uri);
			hideSpinner($("#mffer-admin-input-oauthredirecturi"));
		})
		.catch(function (reason: any) {
			throwError("Unable to get redirect URI: " + reason.toString());
		});
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
	if (!systemData.configuration)
		systemData["configuration"] = loadConfiguration();
	systemData.configuration
		.then((config: string) => loadSettings(JSON.stringify(config)))
		.catch(function () {
			throwError("Unable to get the current configuration");
		});
	$("#mffer-filechooser-pending button").click(function (event) {
		$("#mffer-filechooser-pending").hide();
		switch (event.target.id) {
			case "button-filechooser-confirm":
				showSpinner($("#mffer-admin-input-fileupload"));
				postRequest("importNewData", importText, settings)
					.then((_) => uploadComplete())
					.catch(function () {
						throwError("Unable to import new data");
					});
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
	resetAdminSubmitButton();
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
async function resetAdminSubmitButton(): Promise<void> {
	if (!systemData["texts"]) loadSystemData("texts", loadTexts);
	let texts = await getSystemData("texts");
	let defaultAdminSubmitText: string = "";
	if (texts && texts.defaultAdminSubmitText)
		defaultAdminSubmitText = texts.defaultAdminSubmitText.toString();
	$("#mffer-admin-button-authorize")
		.on("click", adminAuthAndSave)
		.html(defaultAdminSubmitText)
		.removeAttr("disabled");
}
async function adminAuthAndSave() {
	$("#mffer-admin-button-authorize")
		.off("click")
		.attr("disabled", "true")
		.html(bootstrapElements.spinner);
	if (!(await isConfigured())) {
		if (
			isFalseOrEmpty(mfferSettings.oauthId) ||
			isFalseOrEmpty(mfferSettings.oauthSecret)
		) {
			$("#mffer-admin-authorizationerror").html(
				"OAuth ID and OAuth secret are required"
			);
			resetAdminSubmitButton();
		}
	}
	logDebug("Attempting to validate deployment configuration changes");
	$("#mffer-admin-authorizationerror").html("");
	let properties: { [index: string]: string | boolean } = {};
	for (let property of Object.keys(mfferSettings)) {
		if (
			!$("#mffer-admin-input-" + property.toLowerCase()).attr("disabled")
		) {
			properties[property] = mfferSettings[property];
		}
	}
	let propertiesJson = JSON.stringify(properties);
	// because on Apps Script the iframe has
	// 'allow-top-navigation-by-user-activation', the redirection must happen
	// within x seconds of the user clicking or typing something; for most
	// browsers it appears x is 5 but at least for some safari it's 2. Need to
	// make some way to work around this, e.g., prompt user to click on
	// something else when the redirect is ready if it's been too long.
	postRequest("getAdminAuthUrl", propertiesJson)
		.then(function (authUrlMessage: any) {
			if (!authUrlMessage)
				throw new Error(
					"Unable to obtain admin login url: empty result"
				);
			else if (authUrlMessage["error"]) {
				let error: string = "";
				if (authUrlMessage["error_subtype"])
					error =
						"Unable to validate admin configuration: " +
						authUrlMessage.error_subtype.toString() +
						": " +
						authUrlMessage.error.toString();
				else
					error =
						"Unable to validate admin configuration: " +
						authUrlMessage.error.toString();
				throw new Error(error);
			} else if (authUrlMessage["url"]) {
				$("#mffer-admin-button-authorize")
					.on("click", () => {
						$("#mffer-admin-button-authorize")
							.off("click")
							.attr("disabled", "true")
							.html(bootstrapElements.spinner);
						$(document.createElement("a"))
							.attr("href", authUrlMessage.url.toString())[0]
							.click();
					})
					.html("Authorize &amp; Submit")
					.removeAttr("disabled");
			} else {
				throw new Error("Unable to validate admin configuration.");
			}
		})
		.catch(function (error: any) {
			error = error.toString();
			console.error("Unable to obtain admin login URL: " + error);
			resetAdminSubmitButton();
		});
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
	postRequest("getPickerApiKey")
		.then(createGooglePicker)
		.catch(function () {
			throwError("Unable to obtain Google Picker API Key");
		});
}
async function createGooglePicker(apiKey?: string) {
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
	if (!systemData["url"]) loadSystemData("url", loadUrl);
	let url: string = (await getSystemData("url")).toString();
	googlePicker = new google.picker.PickerBuilder()
		.addView(spreadsheetView)
		.hideTitleBar()
		.setOAuthToken(googleOauthToken)
		.setDeveloperKey(pickerApiKey)
		.setOrigin(new URL(url!).origin)
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
			// not sure what's supposed to be below here, as the function isn't given
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
/**
 * Set up the user login system. Checks for served credentials, and if not
 * present attempts to obtain updated credentials using stored user information.
 * If that's not available either, presents the login button.
 * @returns
 */
async function initializeUserLogin(): Promise<void> {
	return new Promise<void>(async (resolve, _) => {
		loadSystemData("loginurl", loadUserLoginUrl);
		loadSystemData("credentials", loadCredentials);
		if (!settings) settings = await initializeSettings();
		let credentials = await getSystemData("credentials");
		if (
			credentials &&
			credentials["user"] &&
			credentials["oauth2.userLogin"]
		) {
			// we've obtained up-to-date user credentials, probably from a Google callback
			logDebug("Up-to-date credentials available on initialization");
		} else if (settings["oauth2.userLogin"]) {
			// there are (possibly old) credentials still in the settings; let's check and maybe update them
			logDebug(
				"Stored credentials; will check validity and update if possible"
			);
			reloadSystemData("credentials", () => {
				return postRequest("updateUserLogin", settings);
			});
			credentials = await getSystemData("credentials");
		} else {
			// there's no current login information
		}
		if (credentials && credentials["oauth2.userLogin"]) {
			// we have a valid login
			logDebug("Credentials confirmed. Storing.");
			await setCredentials(credentials);
			// make the logout button
			$("#mffer-navbar-button-login")
				.on("click", userLogout)
				.html("logout")
				.removeAttr("disabled")
				.show();
		} else {
			// there's no current user login
			await userLogout();
		}
		resolve();
	});
}
async function setCredentials(credentials: any) {
	if (!settings) settings = await initializeSettings();
	settings.user = credentials.user;
	settings["oauth2.userLogin"] = credentials["oauth2.userLogin"];
	settings.isAdmin = credentials.isAdmin;
	if (settings.isAdmin) enableAdminContent();
	if (credentials.hasUserSpreadsheet) settings.hasUserSpreadsheet = true;
	else
		showAlert(
			"Spreadsheet not found",
			"Please select or create a Google Spreadsheet to save your individual data"
		);
	savePageStorage();
}

async function showLoginSpinner() {
	$("#mffer-navbar-button-login")
		.attr("disabled", "true")
		.html(bootstrapElements.spinner)
		.show();
}
function savePageStorage() {
	if (hasLocalStorage()) {
		if (settings != null && settings["oauth2.userLogin"] != null)
			window.localStorage.setItem(
				"oauth2.userLogin",
				settings["oauth2.userLogin"].toString()
			);
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
/**
 * determines the Google url to use for user logins
 * @returns a Promise for the url
 */
async function loadUserLoginUrl() {
	return new Promise<string>(async (resolve, _) =>
		resolve((await postRequest("getUserAuthUrl")).toString())
	);
}
async function userLogout() {
	loadSystemData("loginurl", loadUserLoginUrl);
	hideAdminContent();
	// remove any old login credentials
	if (settings) {
		settings["oauth2.userLogin"] = null;
		settings.isAdmin = null;
		settings.hasUserSpreadsheet = null;
		settings.user = null;
	}
	if (hasLocalStorage()) localStorage.clear();
	// make the login button
	let loginUrl = await getSystemData("loginurl");
	if (loginUrl)
		$("#mffer-navbar-button-login")
			.on("click", () => {
				$(document.createElement("a"))
					.attr("href", loginUrl)[0]
					.click();
			})
			.html("login")
			.removeAttr("disabled")
			.show();
	else throw new Error("Unable to obtain user login url");
}
async function initializePage() {
	await bootstrapify();
	homePage = $("#mffer-contents");
	postRequest("getWebappDatabase").then(loadData).catch(alertLoadFailure);
	$("#mffer-admin-input-oauthid")
		.add("#mffer-admin-input-oauthsecret")
		.attr("disabled", "disabled");
	initializeUserLogin();
	openContents();
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
	postRequest("saveFloorNumber", floorNumber).then(logSuccess).catch(logWarn);
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
	postRequest("csvToTable", text, settings)
		.then(displayTable)
		.catch(displayTableError);
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
