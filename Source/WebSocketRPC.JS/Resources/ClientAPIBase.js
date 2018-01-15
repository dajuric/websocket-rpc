//auto-generated using WebSocketRPC.JS library

//-----outgoing RPCs
var registeredFunctions = [];
function callRPC(funcName, funcArgVals)
{
    if (ws === null) return;

    if (!!registeredFunctions[funcName] === false)
    {
        registeredFunctions[funcName] = { FunctionName: funcName, OnReturn: null, OnError: null /*used by Promise*/ };
    }

    var promise = new Promise((resolve, reject) =>
    {
        registeredFunctions[funcName].OnReturn = resolve;
        registeredFunctions[funcName].OnError = reject;
    });

    ws.send(JSON.stringify({ FunctionName: funcName, Arguments: funcArgVals }));
    return promise;
}

function onRPCResponse(data)
{
    if (!!data.Error && data.Error)
        registeredFunctions[data.FunctionName].OnError(data.Error);
    else
        registeredFunctions[data.FunctionName].OnReturn(data.ReturnValue);
}

//-----incoming RPCs
function onCallRequest(data, onError)
{
    var jsonFName = data.FunctionName[0].toLowerCase() + data.FunctionName.substring(1);
    var isFunc = typeof obj[jsonFName] === 'function';
    if (!isFunc)
    {
        onError({ title: "METHOD_NOT_IMPLEMENTED", summary: "The requested method is not implemented: " + jsonFName + "." });
        return;
    }

    var r = null, errMsg = null;
    try { r = obj[jsonFName].apply(obj, data.Arguments); }
    catch (e) { errMsg = e; }

    if (r === null || r === undefined) r = true;
    ws.send(JSON.stringify({ FunctionName: data.FunctionName, CallId: data.CallId, ReturnValue: r, Error: errMsg })); //TODO: error ?
}

function getAllFunctions(obj)
{
    var funcs = {};
    for (var f in obj)
    {
        if (typeof obj[f] === 'function')
            funcs[f] = obj[f];
    }

    return funcs;
}

//-----common
function onMessage(msg, onError)
{
    var data = null;
    try { data = JSON.parse(msg.data); } catch (e) { }

    var isRPCResponse = (data !== null) && !!registeredFunctions[data.FunctionName] && (data.ReturnValue !== undefined || !!data.Error);
    var isCallRequest = (data !== null) && !!data.Arguments;

    if (isRPCResponse)
        onRPCResponse(data);
    else if (isCallRequest)
        onCallRequest(data, onError);
    else if (obj.onOtherMessage)
        obj.onOtherMessage(msg.data);
}


//-----public methods

/**
* {Establishes the connection with the web-socket server.}
* @param  {func} {Callback called when connection is established. Args: <none>.}
* @param  {func} {Callback called if error happens.               Args: <id, title, summary>.}
* @param  {func} {Callback called when connection is closed.      Args: <id, title, summary>.}
* @return {void} {}    
*/
this.connect = function (onOpen, onError, onClose)
{
    if (!!onOpen === false || typeof onOpen !== 'function')
        throw 'onOpen function callback is missing.';

    if (!!onError === false || typeof onError !== 'function')
        throw 'onError function callback is missing.';

    if (!!onClose === false || typeof onClose !== 'function')
        throw 'onClose function callback is missing.';


    if (!!window.WebSocket === false)
        onError({ id: -1, title: "SOCKET_NOSUPPORT", summary: "This browser does not support Web sockets." });

    //reset
    if (ws)
    {
        ws.close();
        ws = null;
    }

    ws = new WebSocket(url);

    ws.onopen = onOpen;
    ws.onmessage = msg => onMessage(msg, onError);

    ws.onerror = function (err)
    {
        if (ws.readyState === 1)
            onError({ id: -1, title: "SOCKET_ERR", summary: err }); //if normal error (connection error is handled in onclose)
    };

    ws.onclose = function (evt)
    {
        switch (evt.code)
        {
            case 1000:
                onClose({ id: evt.code, closeReason: evt.reason || "normal",  summary: "Websocket connection was closed." });
                break;
            case 1006:
                onClose({ id: evt.code, closeReason: evt.reason || "abnormal", summary: "Websocket connection was closed abnormally." });
                break;
            case 1008:
                onClose({ id: evt.code, closeReason: evt.reason || "policy violation", summary: "Websocket connection was closed due to policy violation." });
                break;
            case 1009:
                onClose({ id: evt.code, closeReason: evt.reason ||"message too big", summary: "Websocket connection was closed due to too large message." });
                break;
            case 3001:
                break; //nothing
            default:
                onClose({ id: evt.code, closeReason: evt.reason || "undefined", summary: "Websocket connection was closed (error code: " + evt.code + ")." });
        }

        ws = null;
    };
};

var ws = null;
var obj = this;

/*
 * Action triggered if the client receives a user-defined message (excluding RPC messages)
 * @param {Message} - message.
*/
this.onOtherMessage = null;

/*
 * Send the message using the underlying websocket connection.
 * @param {Message} - message.
*/
this.send = function (message) { ws.send(message); }

/*
 * Closes the underlying websocket connection.
 * @param {number} - Status code (see https://developer.mozilla.org/en-US/docs/Web/API/CloseEvent#Status_codes for details).
 * @param {string} - Human readable close reason (the max length is 123 bytes / ASCII characters).
*/
this.close = function (code, closeReason)
{
    code = (code === undefined) ? 1000 : code;
    closeReason = closeReason || "";

    ws.close(code, closeReason);
}

/************************** API **************************/