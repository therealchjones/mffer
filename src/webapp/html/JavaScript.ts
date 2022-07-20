"use strict";

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
 * the url in the browser location bar; iff being served from Google Apps
 * Script this does not include any query string or hash
 */
var url: string | null = null;
/**
 * the hostname if the page is being served from a custom domain; `null` if
 * it's being served from Google Apps Script
 */
var customDomain: string | null = null;
var configured: boolean | null = null;
var defaultAdminSubmitText: string = "";
document.addEventListener("DOMContentLoaded", start);
async function start() {
	await checkUrl();
	getTexts();
	checkDeployment();
	await checkParameters();
	if (await isConfigured()) setConfigured();
	else setNotConfigured();
}
async function getTexts(): Promise<void> {
	return new Promise<void>((resolve, _) => {
		defaultAdminSubmitText =
			document.getElementById("mffer-admin-button-authorize")
				?.innerHTML || "";
		resolve();
	});
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
function isOnGas(): boolean {
	return window.location.hostname.endsWith(".googleusercontent.com");
}
function isSetupOnly(): boolean {
	if (deploymentId && isOnGas()) return true;
	else return false;
}
async function postRequest(functionName: string, ...args: any[]): Promise<any> {
	let postUrl: string;
	if (deploymentId)
		postUrl =
			"https://script.google.com/macros/s/" + deploymentId + "/exec";
	else {
		if (!url) await checkUrl();
		if (!url) throw new Error("Unable to get url for posting requests");
		postUrl = url;
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
	else return response.json();
}
function throwFetchError(reason: any): never {
	throw new Error("Unable to fetch data: " + reason?.toString());
}
async function checkParameters(): Promise<void> {
	return new Promise<void>(async function (resolve, _) {
		console.debug("Checking URL parameters");
		if (!url) await checkUrl();
		let urlObj: URL;
		let parameters: { [key: string]: string | null } = {};
		if (!url) throw new Error("Unable to get URL");
		else {
			urlObj = new URL(url);
			if (isOnGas()) parameters = await getGasParameters();
			else {
				for (const key of urlObj.searchParams.keys()) {
					parameters[key] = urlObj.searchParams.get(key);
				}
			}
		}
		if (Object.keys(parameters).includes("state")) {
			console.debug("Sending callback parameters to Apps Script");
			// some sort of callback (or reload)
			// if already logged in, check to see if it's the same login or a state that's already been used?
			// determine whether we're looking for a user or admin login?
			let response: { parameter: { [key: string]: string | null } } = {
				parameter: parameters,
			};
			// send to server
			postRequest("processParameters", response)
				.then(function (credentials: any) {
					// figure out what needs to handle the returned credentials
					// possible "success" responses (e.g., no error thrown by method)
					// admin login success: { config: <getConfig() response> }
					// admin login failure: { error: <error message> }
					// user login success: { user: <user data for storage> }
					// user login failure: { error: <error message> }
					if (!credentials) {
						console.debug("No credentials received from server");
						resolve();
					}
					let keys = Object.keys(credentials);
					if (keys.length == 0)
						console.debug(
							"Empty credentials object received from server"
						);
					else if (keys.includes("user")) {
						console.debug("Received user credentials");
						setLoginStatus(credentials);
					} else if (keys.includes("error"))
						console.warn("Invalid login: " + credentials.error);
					else if (keys.includes("config")) {
						console.debug("Deployment admin settings updated");
						loadSettings(JSON.stringify(credentials.config));
					} else
						console.error(
							"Invalid credentials received from server"
						);
					resolve();
				})
				.catch((error: any) => {
					console.error(
						"Unable to validate parameters: " + error?.toString()
					);
				});
		} else if (Object.keys(parameters).includes("page")) {
			// (future work) go directly to a specific page
			resolve();
		} else {
			// nothing to do
			resolve();
		}
	});
}
/**
 * Ensure there is a deploymentID setting & sanity checks pass.
 * @throws Error if something appears amiss with the deployment.
 */
async function checkDeployment() {
	let deploymentExplainer =
		"; the project was probably not deployed properly. See https://dev.mffer.org/.";
	if (deploymentId === undefined || deploymentId === null) {
		throw new Error("Undefined deployment ID" + deploymentExplainer);
	} else if (deploymentId == "") {
		// the web page was built for Google Apps Script
		if (customDomain)
			throw new Error(
				"Empty deployment ID set with custom domain" +
					deploymentExplainer
			);
	} else {
		// the web page was built for a custom domain
		if (!customDomain)
			throw new Error(
				"Deployment ID set without custom domain" + deploymentExplainer
			);
	}
}
async function checkUrl() {
	return new Promise<void>(function (resolve, _) {
		console.debug("Checking URL");
		if (isOnGas()) {
			google.script.run
				.withSuccessHandler(async function (gasUrl: string) {
					url = gasUrl;
					customDomain = null;
					resolve();
				})
				.withFailureHandler((error) => throwError(error.toString()))
				.getUrl();
		} else {
			url = window.location.href;
			customDomain = window.location.hostname;
			resolve();
		}
	});
}
async function getGasParameters(): Promise<{ [key: string]: string }> {
	return new Promise(function (resolve, _) {
		console.debug("Getting parameters from Apps Script");
		if (!isOnGas())
			throw new Error(
				"Unable to obtain parameters using getGasParameters when not serving from Apps Script"
			);
		google.script.url.getLocation((location) =>
			resolve(location.parameter)
		);
	});
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
async function initializeServerStorage() {
	if (serverStorage != null)
		throw new Error("server storage is already configured");
	return postRequest("getConfig")
		.then(function (response: string) {
			let pageStorageText = response.trim();
			if (pageStorageText) serverStorage = JSON.parse(pageStorageText);
			else serverStorage = {};
		})
		.catch(() => throwError("Unable to get server storage"));
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
		if (configured == null)
			configured = await postRequest("isConfigured")
				.then((result: any) => result === true)
				.catch((reason) => {
					console.error(
						`Unable to check configuration: ${reason.toString()}`
					);
					return false;
				});
		resolve(configured);
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
	console.debug("Starting webapp configuration");
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
	console.debug("Switching to admin configuration page");
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
	postRequest("getConfig")
		.then((config: string) => loadSettings(config))
		.catch(function () {
			throwError("Unable to get the current configuration");
		});
	$("#mffer-filechooser-pending button").click(function (event) {
		$("#mffer-filechooser-pending").hide();
		switch (event.target.id) {
			case "button-filechooser-confirm":
				showSpinner($("#mffer-admin-input-fileupload"));
				postRequest("importNewData", workingStorage)
					.then(uploadComplete)
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
function resetAdminSubmitButton(): void {
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
	console.debug("Attempting to validate deployment configuration changes");
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
		.then(function (authUrl: any) {
			authUrl = authUrl.toString();
			if (!authUrl)
				throw new Error(
					"Unable to obtain admin login url: empty result"
				);
			$("#mffer-admin-button-authorize")
				.on("click", () => {
					$("#mffer-admin-button-authorize")
						.off("click")
						.attr("disabled", "true")
						.html(bootstrapElements.spinner);
					$(document.createElement("a"))
						.attr("href", authUrl)[0]
						.click();
				})
				.html("Authorize &amp; Submit")
				.removeAttr("disabled");
		})
		.catch(function (error: any) {
			error = error.toString();
			throwError("Unable to obtain admin login URL: " + error);
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
function showLoginSpinner() {
	$("#mffer-navbar-button-login")
		.attr("disabled", "true")
		.html(bootstrapElements.spinner)
		.show();
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
	// but what if it is present?
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
	postRequest("getUserLoginStatus", storageText)
		.then(setLoginStatus)
		.catch(function () {
			throwError("Unable to get login status");
		});
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
async function resetLogin() {
	return new Promise<void>((resolve, _) => {
		$("#mffer-navbar-button-login")
			.html("login")
			.on("click", userLogin)
			.show();
	});
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
	postRequest("getUserAuthUrl", JSON.stringify(workingStorage))
		.then(function (url: any) {
			$(document.createElement("a"))
				.attr("href", url.toString())[0]
				.click();
		})
		.catch(function () {
			throwError("Unable to get user login URL");
		});
}
async function checkUserLoginUrl() {
	return postRequest("getUserAuthUrl").then((loginUrl: any): void => {
		$("#mffer-navbar-button-login").on("click", () =>
			$(document.createElement("a"))
				.attr("href", loginUrl.toString())[0]
				.click()
		);
	});
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
	postRequest("getWebappDatabase").then(loadData).catch(alertLoadFailure);
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
	postRequest("csvToTable", text, workingStorage)
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
