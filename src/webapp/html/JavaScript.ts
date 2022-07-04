<script
	defer
	id="jquery-js"
	src="https://ajax.googleapis.com/ajax/libs/jquery/3.6.0/jquery.min.js"
	crossorigin="anonymous"
></script>
<script
	defer
	id="bootstrap-js"
	src="https://cdn.jsdelivr.net/npm/bootstrap@5.1.0/dist/js/bootstrap.bundle.min.js"
	integrity="sha384-U1DAWAznBHeqEIlVSCgzq+c9gqGAJn5c/t99JyeKa9xxaYpSvHU5awsuZVVFIhvj"
	crossorigin="anonymous"
></script>
<div id="mffer-storage" class="start-inactive">
	<?!= JSON.stringify(storage.getProperties()); ?>
</div>
<script id="mffer-js">
	"use strict";
	var debug = true; // debug = true outputs more to the console
	var mainPage = null;
	document.addEventListener("DOMContentLoaded", start);
	function start() {
		checkConfigured();
	}
	async function setConfigured() {
		await Promise.all([initializeStorage(), bootstrapify()]);
		mainPage = $("#mffer-contents");
		initializePage();
	}
	function setNotConfigured() {
		mainPage = $("#mffer-welcome");
		initializeSetup();
	}

	/** Various memory stores for volatile and persistent memory, complicated by
	 * the inability to use window.localStorage directly for apps script since
	 * it presents user code in an iframe from a different domain. Each is
	 * scoped differently, with more narrow scopes overriding more broad ones:
	 *
	 * mfferStorage: volatile, in memory, resets with page load
	 * pageStorage: copied from server for current page load, not copied back
	 * localStorage: script's window.localStorage, but due to third-party
	 *               storage restrictions persists only for current session
	 * frameStorage: stored by window.top.localStorage when in iframe
	 */
	var mfferStorage = null;
	var pageStorage = null;
	// var localStorage = null; // should already be defined if supported
	var frameStorage = null;
	async function initializeStorage() {
		if (mfferStorage != null)
			throw new Error("Storage has already been initialized");
		mfferStorage = {};
		let frameStoragePromise = timeout(initializeFrameStorage(), 1000);
		let localStoragePromise = initializeLocalStorage();
		let pageStoragePromise = initializePageStorage();

		const results = await Promise.allSettled([
			frameStoragePromise,
			localStoragePromise,
			pageStoragePromise,
		]);
		if (results[0] == "fulfilled")
			mfferStorage = { ...frameStorage.storage };
		if (results[1] == "fulfilled")
			mfferStorage = { ...mfferStorage, ...localStorage };
		if (results[2] == "fulfilled")
			mfferStorage = { ...mfferStorage, ...pageStorage };
	}
	function initializeFrameStorage() {
		return new Promise(function (resolve, reject) {
			if (frameStorage != null)
				throw new Error("Frame storage is already configured.");
			else {
				initialListener = function (messageEvent) {
					let messageName = getMessageName(messageEvent);
					if (messageName != "frameStorageSetup")
						throw new Error("Not a frame storage setup message");
					frameStorage = {};
					frameStorage.origin = messageEvent.origin.toString();
					frameStorage.server = messageEvent.source;
					frameStorage.storeItem = frameStorageStore;
					frameStorage.retrieveItem = frameStorageRetrieve;
					if (messageEvent.data.storage === undefined)
						frameStorage.storage = {};
					else frameStorage.storage = messageEvent.data.storage;
					if (messageEvent.data.callbackUrl)
						mfferStorage.callbackUrl =
							messageEvent.data.callbackUrl;
					window.removeEventListener("message", initialListener);
					window.addEventListener(
						"message",
						receiveFrameStorageMessage
					);
					resolve();
				};
				window.addEventListener("message", initialListener);
				window.top.postMessage("frameStorageSetup", "*");
			}
		});
	}
	function initializeLocalStorage() {
		return new Promise(function (resolve, reject) {
			if (!window.localStorage)
				throw new Error("No local storage is available.");
			resolve();
		});
	}
	function initializePageStorage() {
		return new Promise(function (resolve, reject) {
			if (pageStorage != null)
				throw new Error("Page storage is already configured.");
			google.script.run
				.withFailureHandler(throwError("Unable to get server storage"))
				.withSuccessHandler(function () {
					let pageStorageText = response.trim();
					if (pageStorageText)
						pageStorage = JSON.parse(pageStorageText);
					else pageStorage = {};
					resolve();
				})
				.getStoredProperties();
		});
	}
	function getMessageName(messageEvent) {
		if (messageEvent == null || messageEvent.data == null)
			throw new Error("Invalid message received");
		else {
			if (messageEvent.data.name === undefined)
				return messageEvent.data.toString();
			else return messageEvent.data.name.toString();
		}
	}
	var initialListener = null;
	function timeout(promise, time) {
		return Promise.race([
			promise,
			new Promise(function (resolve, reject) {
				setTimeout(reject, time);
			}),
		]);
	}
	var shadowlandDatabase = null;
	var googlePicker = null;
	var googleOauthToken = null;
	var mfferSettings = {
		oauthId: "",
		oauthSecret: "",
		pickerApiKey: "",
	};
	var importText = null;
	var bootstrap = {
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
			.withSuccessHandler(function (response) {
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
				.withSuccessHandler(function (response) {
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
	function storeItem(key, value) {
		if (frameStorage != null) {
			frameStorage.storeItem(key, value);
		} else if (hasLocalStorage()) {
			window.localStorage.setItem(key, value);
		} else {
			throwError(
				"No storage medium available to store '" + key.toString() + "'"
			);
		}
	}
	function retrieveItem(key) {
		if (frameStorage != null) {
			return frameStorage.retrieveItem(key);
		} else if (hasLocalStorage()) {
			return window.localStorage.getItem(key);
		} else {
			throwError(
				"No storage medium available from which to retrieve '" +
					key.toString() +
					"'"
			);
		}
	}
	function frameStorageRetrieve(key) {
		if (frameStorage == null) {
			throwError("Frame storage is not available.");
		} else {
			frameStorage.server.postMessage(
				{
					name: "frameStorageRetrieve",
					key: "key",
				},
				frameStorage.origin
			);
		}
	}
	function frameStorageStore(key, value) {
		if (frameStorage == null) {
			throwError("Frame storage is not available.");
		} else {
			frameStorage.server.postMessage(
				{
					name: "frameStorageStore",
					key: key,
					value: value,
				},
				frameStorage.origin
			);
		}
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
		pages.find("a[target='_blank']").append(bootstrap.boxarrow);
		bootstrapifyAdmin();
	}
	function bootstrapifyAdmin() {
		let lis = $("#mffer-admin li");
		let spinner = bootstrap.spinner;
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
	function showLoading(message = null) {
		$("#loading-notes").text(message);
		hidePages();
		$("#mffer-spinner").show();
	}
	function hideLoading() {
		$("#mffer-spinner").hide();
		$("#loading-notes").text(null);
	}
	function hidePages() {
		$("body")
			.children('[id^="mffer-"]')
			.filter(':not(".always-active")')
			.filter(':not("#mffer-alert")')
			.hide();
	}
	function receiveFrameStorageMessage(messageEvent) {
		let messageName = getMessageName();
		switch (messageName) {
			case "frameStorageRetrieve":
				if (
					messageEvent.data.key === undefined ||
					messageEvent.data.value === undefined
				)
					debugLog("Frame storage 'retrieve' message has no data");
				if (mfferStorage == null) mfferStorage = {};
				mfferStorage[messageEvent.data.key] = messageEvent.data.value;
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
				debugLog(
					"Received unknown message type: '" + messageName + "'"
				);
				break;
		}
		return;
	}
	function debugLog(message) {
		if (debug) console.log(message);
	}
	function showAlert(messageOrTitle, message = null) {
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
	function showSpinner(element) {
		getSpinner(element).show();
	}
	function hideSpinner(element) {
		getSpinner(element).hide();
	}
	function getSpinner(element) {
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
			.click(openAdmin)
			.html("setup mffer")
			.removeAttr("disabled");
	}
	function openContents() {
		hidePages();
		mainPage.show();
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
			.withSuccessHandler(function (uri) {
				$("#mffer-admin-input-oauthredirecturi").val(uri);
				hideSpinner($("#mffer-admin-input-oauthredirecturi"));
			})
			.withFailureHandler(function () {
				throwError("Unable to get redirect URI");
			})
			.getRedirectUri();
		$("#mffer-admin")
			.find("input[type='text']")
			.change(function (event) {
				showSpinner(this);
				let text = $(this).val().toString().trim();
				let inputName = event.currentTarget.id;
				let shortName = inputName.replace(/^mffer-admin-input-/, "");
				for (let property in mfferSettings) {
					if (property.toLowerCase() == shortName)
						mfferSettings[property] = text;
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
							.append(bootstrap.boxarrow);
						break;
					default:
						break;
				}
				hideSpinner(this);
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
						.importNewData(
							importText,
							JSON.stringify(mfferStorage)
						);
					break;
				case "button-filechooser-cancel":
					$("#mffer-filechooser-pending").hide();
					$("#button-filechooser-confirm").attr(
						"disabled",
						"disabled"
					);
					$("#mffer-admin-input-fileupload").val(null);
					$("#mffer-admin-input-fileupload").change();
					$("#mffer-admin").show();
					break;
				default:
					throwError("Unable to identify button clicked");
			}
		});
		$("#mffer-admin-input-fileupload").change(function () {
			if (this.files != null && this.files.length != 0) {
				if (this.files.length > 1) {
					throwError("Selection of multiple files is not supported.");
				}
				$("#mffer-filechooser-pending").show();
				let file = this.files[0];
				let fileReader = new FileReader();
				fileReader.onload = function () {
					importText = fileReader.result;
					let headers = getFirstLines(importText);
					$("#mffer-filechooser-textdisplay").html(
						"<pre>\n" + headers + "\n</pre>"
					);
					$("#mffer-filechooser-tabledisplay").html(
						csvToTable(headers, JSON.stringify(mfferStorage))
					);
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
	function isFalseOrEmpty(check) {
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
		$("#mffer-admin-authorizationerror").html(null);
		$("#mffer-admin-button-authorize").click(null);
		$("#mffer-admin-button-authorize").html(bootstrap.spinner);
		let properties = {};
		for (let property in mfferSettings) {
			if (
				!$("#mffer-admin-input-" + property.toLowerCase()).attr(
					"disabled"
				)
			) {
				properties[property] = mfferSettings[property];
			}
		}
		let propertiesJson = JSON.stringify(properties);
		google.script.run
			.withFailureHandler(function (error) {
				throwError("Unable to obtain admin login URL: " + error);
			})
			.withSuccessHandler(function (url) {
				if (isFalseOrEmpty(url))
					throw new Error("Unable to obtain admin login url");
				$(document.createElement("a")).attr("href", url)[0].click();
			})
			.getAdminAuthUrl(propertiesJson);
	}
	function throwError(error) {
		$("#mffer-spinner h2").html("Error");
		let errorMessage =
			"<p>" +
			error +
			"</p><p>More information may be available in your browser's JavaScript console or in Google Scripts execution logs.</p>";
		$("#loading-notes").html(errorMessage);
		throw error;
	}
	function loadSettings(settingString = null) {
		let currentSettings = JSON.parse(settingString);
		for (let property in currentSettings) {
			mfferSettings[property] = currentSettings[property];
		}
		for (let property in mfferSettings) {
			let input = $("#mffer-admin-input-" + property.toLowerCase());
			if (input.length != 0) {
				let text = null;
				if (!isFalseOrEmpty(mfferSettings[property])) {
					text = mfferSettings[property];
					if (mfferSettings[property] === true) text = "[set]";
				}
				if (isFalseOrEmpty(input.val())) input.val(text);
				else mfferSettings[property] = input.val();
				hideSpinner(input);
				input.change();
			}
		}
	}
	function loadPickerApi(token) {
		if (token == null) throwError("Unable to obtain authorization.");
		googleOauthToken = token;
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
	function createGooglePicker(apiKey) {
		if (apiKey != null) {
			pickerApiKey = apiKey;
		}
		let spreadsheetView = new google.picker.DocsView(
			google.picker.ViewId.SPREADSHEETS
		);
		spreadsheetView
			.setIncludeFolders(true)
			.setMode(google.picker.DocsView.LIST);
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
	function pickerCallback(data) {
		if (
			data[google.picker.Response.ACTION] == google.picker.Action.PICKED
		) {
			if (
				data[google.picker.Response.DOCUMENTS] != null &&
				data[google.picker.Response.DOCUMENTS][0] != null
			) {
				$("#mffer-new-get-database").hide();
				showLoading("Checking file");
				google.host.run
					.withSuccessHandler(null)
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
		$("#mffer-navbar-button-login").html(bootstrap.spinner).show();
	}
	function checkLocalStorage() {
		if (!mfferStorage) mfferStorage = {};
		if (hasLocalStorage()) {
			if (window.localStorage.getItem("oauth2.userLogin"))
				mfferStorage["oauth2.userLogin"] =
					window.localStorage.getItem("oauth2.userLogin");
		}
	}
	function checkUserLogin() {
		showLoginSpinner();
		checkPageStorage();
		if (!mfferStorage["oauth2.userLogin"]) {
			openContents();
			// in-page storage did not include new user data; check for
			// persistent old user data
			checkLocalStorage();
			if (!mfferStorage["oauth2.userLogin"]) {
				// there's no user data at all
				resetLogin();
			} else {
				// there was persistent old user data, but it needs to be
				// updated and/or checked
				updateLogin(JSON.stringify(mfferStorage));
			}
		}
	}
	function checkPageStorage() {
		if (pageStorage.adminAuthError) {
			// failed admin login when trying to change settings
			// TODO: #147 consider whether admin auth error should return
			// attempted auth settings
			userLogout();
			showAlert("Administrator authentication error");
			openContents();
		} else if (pageStorage["oauth2.adminLogin"]) {
			// logged in to change settings; will go back to admin page but
			// also login as "regular" user
			mfferStorage["oauth2.userLogin"] = pageStorage["oauth2.adminLogin"];
			updateLogin(JSON.stringify(mfferStorage));
			enableAdminContent();
			openAdmin();
		} else if (pageStorage.userAuthError) {
			// failed standard user login
			userLogout();
			showAlert("User authentication error");
			openContents();
		} else if (pageStorage["oauth2.userLogin"]) {
			// logged in as standard user
			setLoginStatus(JSON.stringify(pageStorage));
			openContents();
		}
	}
	function updateLogin(storageText) {
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
				mfferStorage != null &&
				mfferStorage["oauth2.userLogin"] != null
			)
				window.localStorage.setItem(
					"oauth2.userLogin",
					mfferStorage["oauth2.userLogin"]
				);
		}
	}
	function resetLogin() {
		$("#mffer-navbar-button-login").html("login").click(userLogin).show();
	}
	function setLoginStatus(storageText) {
		if (!storageText) {
			// user does not (currently) have access
			if (hasLocalStorage()) localStorage.clear();
			resetLogin();
		} else {
			let newStorage = JSON.parse(storageText);
			mfferStorage = {};
			mfferStorage["oauth2.userLogin"] = newStorage["oauth2.userLogin"];
			if (newStorage.adminUser) {
				mfferStorage.adminUser = true;
				enableAdminContent();
			} else hideAdminContent();
			if (newStorage.hasUserSpreadsheet)
				mfferStorage.hasUserSpreadsheet = true;
			else
				showAlert(
					"Spreadsheet not found",
					"Please select or create a Google Spreadsheet to save your individual data"
				);
			savePageStorage();
			$("#mffer-navbar-button-login")
				.html("logout")
				.click(userLogout)
				.show();
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
			.withSuccessHandler(function (url) {
				$(document.createElement("a")).attr("href", url)[0].click();
			})
			.getUserAuthUrl(mfferStorage);
	}
	function userLogout() {
		hideAdminContent();
		mfferStorage = null;
		pageStorage = null;
		$("#mffer-storage").html(null);
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
	function loadData(database) {
		if (database == null) alertLoadFailure();
		hideLoading();
	}

	// reset the page settings and info for a given floor number, on the current
	// floor (if set to null or undefined), or from the database (if set
	// to anything that isNaN)
	function resetPage(floorNumber) {
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
	function getCurrentFloor(floorNumber) {
		let currentFloor = 0;
		if (floorNumber == null) {
			// determine the floor number from the page
			// if unable to find, set to NaN
		} else if (isNaN(floorNumber)) {
			try {
				currentFloor = shadowlandDatabase[28][1];
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
		} else {
			saveFloorNumber(currentFloor);
			return currentFloor;
		}
	}

	function markProgress(text, jobNumber = null) {
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
	function alertWarn(text) {
		alertError(text);
	}

	// for issues that require user intervention
	function alertError(text) {
		$("#mffer-alert").html(text);
		$("#mffer-alert").show();
	}

	// for issues that are unrecoverable
	function alertHalt(text) {
		window.alert(text);
		throw new Error("Unrecoverable: " + text);
	}

	function alertLoadFailure(error = null) {
		if (error == null) {
			alertError("Unable to load the database.");
		} else {
			alertError(
				"Error loading the database: " +
					error.name +
					": " +
					error.message
			);
		}
		hideLoading();
	}
	function alertSaveFailure(error, userObj) {
		alertError(
			"Error saving the battle record: " +
				error.name +
				": " +
				error.message
		);
	}

	function getJobText(jobNumber) {
		// get the text associated with this jobNumber from the message queue
		return jobNumber;
	}

	function saveFloorNumber(floorNumber) {
		let jobNumber = markProgress("Saving current floor");
		google.script.run
			.withFailureHandler(logWarn)
			.withSuccessHandler(logSuccess)
			.withUserObject(jobNumber)
			.saveFloorNumber(floorNumber);
		return floorNumber;
	}
	function getFirstLines(text) {
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
	function csvToTable(text) {
		google.script.run
			.withSuccessHandler(displayTable)
			.withFailureHandler(displayTableError)
			.csvToTable(text, JSON.stringify(mfferStorage));
		return "<p>" + bootstrap.spinner + "</p>";
	}
	function displayTable(text) {
		let result = $("<div>" + text + "</div>");
		let storage = {};
		if (
			result.find("#mffer-admin-filechooser-pending-storage").length > 0
		) {
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
	function displayTableError(error) {
		$("#mffer-filechooser-tabledisplay").html(
			"<p>Parsing failed:</p>\n<p>" + error.message + "</p>"
		);
	}
</script>
