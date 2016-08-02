/// <reference path="Libraries/webApiRest.js" />
/// <reference path="Libraries/knockout340.js" />
(function (global, webAPI, ko) {
    'use strict';

    global.Campmon = global.Campmon || {};
    global.Campmon.ConfigurationPage = global.Campmon.ConfigurationPage || (function () {
        function CampmonViewModel(config) {
            var self = this;

            self.clients = ko.observableArray();
            self.clientLists = ko.observableArray();

            self.selectedClient = ko.observable();
            self.selectedList = ko.observable();
            self.hasConnectionError = ko.observable(false);
            self.isClientSelected = ko.observable(false);

            self.selectedPrimaryEmail = ko.observable(config.PrimaryEmailField)
        }

        function init() {
            var config = loadConfig();
            var vm = new CampmonViewModel(config);

            vm.selectedClient.subscribe(function (selectedClient) {
                CMPlugin.executeAction('getclientlist', selectedClient)
                    .then(function (result) {
                        //TODO: If no client lists default to Sync to New List Option
                        vm.clientLists(JSON.parse(result.body.OutputData));
                    }, function (error) {
                        alert("Error retrieving lists for selected client.");
                    });
            });

            ko.applyBindings(vm);

            Campmon.Plugin.executeAction('loadmetadata', null)
                .then(function (result) {
                    vm.clients(JSON.parse(result.body.OutputData));
                    vm.isClientSelected(true);
                }, function (error) {
                    vm.hasConnectionError = true;
                    console.log(JSON.parse(error.response.text));
                });
        }

        function loadConfig() {
            // TODO: Implement loading functionality
            return {
                AccessToken: "",
                BulkSyncData: "",
                BulkSyncInProgress: false,
                ClientId: "",
                ClientName: "",
                ListId: "",
                ListName: "",
                SetUpError: "",
                SyncDuplicateEmails: false,
                SyncFields: [],
                SyncViewId: "",
                SyncViewName: "",        
                SubscriberEmail: "778230000"
            };
        }

        return {
            init: init
        };
    })();

    global.Campmon.Plugin = global.Campmon.Plugin || (function () {
        var _actionName = 'campmon_ExecuteOperationAction';

        var pluginInput = function (operation, data) {
            return {
                OperationName: operation,
                InputData: data
            };
        }

        var executeAction = function (action, data) {
            return webAPI.REST.executeUnboundAction(_actionName, pluginInput(action, data), null);
        }

        return {
            executeAction: executeAction
        }
    })();

})(this, webAPI, ko);
