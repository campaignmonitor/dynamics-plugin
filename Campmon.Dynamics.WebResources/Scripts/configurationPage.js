/// <reference path="Libraries/webApiRest.js" />
/// <reference path="Libraries/knockout340.js" />
var CM = (function () {
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


var clients = [];
CM.executeAction('getclientlist', 'testdata').
 then(function (result) {
     debugger;
     clients.push(JSON.parse(result.body.OutputData));
 }, function (error) {
     debugger;
     console.log(JSON.parse(error.body.OutputData));
 });


// Activates knockout.js

function ConfigurationViewModel() {
    var self = this;
    self.firstName = "Adam";
    self.clientList = ko.observableArray(clients);

}
ko.applyBindings(new ConfigurationViewModel());