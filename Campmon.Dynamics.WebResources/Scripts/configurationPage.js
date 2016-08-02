/// <reference path="Libraries/webApiRest.js" />
/// <reference path="Libraries/knockout340.js" />
(function (global, webAPI, ko) {
    'use strict';

    global.Campmon = global.Campmon || {};
    global.Campmon.ConfigurationPage = global.Campmon.ConfigurationPage || (function () {
        function CampmonViewModel() {
            var self = this;

            self.clients = ko.observableArray();
            self.clientLists = ko.observableArray();

            self.selectedClient = ko.observable();
            self.selectedList = ko.observable();
            self.hasConnectionError = ko.observable(false);
            self.isClientSelected = ko.observable(false);
        }

        function init() {
            var vm = new CampmonViewModel();

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
                    var config = JSON.parse(result.body.OutputData);
                    if (config.Error) {
                        alert(config.Error);
                    }
                    vm.clients(config.Clients);
                    vm.isClientSelected(true);
                }, function (error) {
                    vm.hasConnectionError = true;
                    console.log(JSON.parse(error.response.text));
                });
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
