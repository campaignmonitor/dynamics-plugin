/// <reference path="Libraries/webApiRest.js" />
/// <reference path="Libraries/knockout340.js" />

(function () {
    "use strict";

    var CampaignMonitorConfig = (function () {
        
        // todo: load config, then the observables may be set up based on CampaignMonitorConfig.<attr>        
        
        function saveConfig() {
            // todo: Implement
        }

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
            SubscriberEmail: "778230000",
            save: saveConfig
        };
    }());
    
    function CampMonViewModel(config) {
        var self = this;

        self.clients = ko.observableArray();
        self.clientLists = ko.observableArray();

        self.selectedClient = ko.observable();
        self.selectedList = ko.observable();
        self.hasConnectionError = ko.observable(false);
        self.isClientSelected = ko.observable(false);

        self.selectedPrimaryEmail = ko.observable(config.SubscriberEmail);
    }

    var cm = new CampMonViewModel(CampaignMonitorConfig);
    ko.applyBindings(cm);

    var CMPlugin = (function () {
        const _pluginAction = 'campmon_ExecuteOperationAction';
        var pluginInput = function (operation, data) {
            return {
                OperationName: operation,
                InputData: data
            };
        }
        var executeAction = function (action, data) {
            return webAPI.REST.executeUnboundAction(_pluginAction, pluginInput(action, data), null);
        }
        return {
            executeAction: executeAction
        }
    })();

    CMPlugin.executeAction('getclients', null).
     then(function (result) {
         cm.clients(JSON.parse(result.body.OutputData));
         cm.isClientSelected(true);
     }, function (error) {
         cm.hasConnectionError = true;
         console.log(JSON.parse(error.response.text));
     });

    cm.selectedClient.subscribe(function (selectedClient) {
        CMPlugin.executeAction('getclientlist', selectedClient).
        then(function (result) {
            //TODO: If no client lists default to Sync to New List Option
            cm.clientLists(JSON.parse(result.body.OutputData));
        }, function (error) {
            alert("Error retrieving lists for selected client.");
        });
    });
}());