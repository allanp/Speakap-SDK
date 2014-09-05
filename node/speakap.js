"use strict";

var crypto = require("crypto");
var _ = require("lodash");

var SIGNATURE_WINDOW_SIZE = 60 * 1000; // ms

function percentEncode(string) {

    return encodeURIComponent(string).replace("!", "%21").replace("'", "%27")
                                     .replace("(", "%28").replace(")", "%29")
                                     .replace("*", "%2A");
}

/**
 * Generates the signed request string from the parameters.
 *
 * @param params Object containing POST parameters passed during the signed request.
 *
 * @return Query string containing the parameters of the signed request.
 *
 * Note this method does not calculate a signature; it simply generates the signed request from
 * the parameters including the signature.
 */
function signedRequest(params) {

    var keys = _.without(_.keys(params), "signature");
    keys.sort();
    keys.push("signature");
    return _.map(keys, function(key) {
        return percentEncode(key) + "=" + percentEncode(params[key]);
    }).join("&");
}

/**
 * Validates the signature of a signed request.
 *
 * @param params Object containing POST parameters passed during the signed request.
 * @param appSecret App secret the parameters should've been signed with.
 *
 * Throws an exception if the signature doesn't match or the signed request is expired.
 */
function validateSignature(params, appSecret) {

    var signature = params.signature;

    var keys = _.keys(params);
    keys.sort();
    var queryString = _.map(_.without(keys, "signature"), function(key) {
        return percentEncode(key) + "=" + percentEncode(params[key]);
    }).join("&");

    var hmac = crypto.createHmac("sha256", appSecret);
    var computedHash = hmac.update(queryString).digest("base64");

    if (computedHash !== signature) {
        throw new Error("Invalid signature: " + queryString);
    }

    var issuedAt = new Date(params.issuedAt).getTime();
    if (Date.now() > issuedAt + SIGNATURE_WINDOW_SIZE) {
        throw new Error("Expired signature");
    }
}

/**
 * Speakap API wrapper.
 *
 * You should instantiate the Speakap API as follows:
 *
 *   var Speakap = require("speakap");
 *   var speakapApi = new Speakap.API({
 *       scheme: "https",
 *       hostname: "api.speakap.io",
 *       appId: MY_APP_ID,
 *       appSecret: MY_APP_SECRET
 *   });
 *
 * Obviously, MY_APP_ID and MY_APP_SECRET should be replaced with your actual App ID and secret (or
 * by constants containing those).
 *
 * After you have instantiated the API wrapper, you can perform API calls as follows:
 *
 *   speakapApi.get("/networks/" + networkId + "/user/" + userId + "/", function(error, result) {
 *       if (error) {
 *           // handle error
 *       } else {
 *           // do something with result
 *       }
 *   });
 *
 *   speakapApi.post("/networks/" + networkId + "/messages/", {
 *       "body": "test 123",
 *       "messageType": "update",
 *       "recipient": { "type": "network", "EID": networkId }
 *   }, function(error, result) {
 *       if (error) {
 *           // handle error
 *       } else {
 *           // do something with result
 *       }
 *   });
 *
 * The result parameter is an already parsed reply in case of success. The error parameter is an
 * object containing code and message properties in case of an error.
 */
function API(config) {

    if (config.scheme !== "http" && config.scheme !== "https") {
        throw new Error("Speakap scheme should be http or https");
    }

    this.scheme = require(config.scheme);
    this.hostname = config.hostname;
    this.appId = config.appId;
    this.appSecret = config.appSecret;

    this.accessToken = this.appId + "_" + this.appSecret;
}

_.extend(API.prototype, {

    /**
     * Performs a DELETE request to the Speakap API.
     *
     * @param path The path of the REST endpoint, including optional query parameters.
     * @param callback Callback that receives the result of the request. It received two parameters:
     *                 error - Error object with code and message properties if the request failed.
     *                 result - Parsed JSON response.
     *
     * @return A jQuery promise object with is .
     *
     * Example:
     *
     *   speakapApi["delete"]("/networks/" + networkId +
     *                        "/messages/" + messageId + "/", function(error, result) {
     *       if (error) {
     *           // handle error
     *       } else {
     *           // do something with result
     *       }
     *   });
     */
    "delete": function(path, callback) {

        this._request("DELETE", path, null, callback);
    },

    /**
     * Performs a GET request to the Speakap API.
     *
     * @param path The path of the REST endpoint, including optional query parameters.
     * @param callback Callback that receives the result of the request. It received two parameters:
     *                 error - Error object with code and message properties if the request failed.
     *                 result - Parsed JSON response.
     *
     * @return A tuple containing the parsed JSON reply (in case of success) and an error object
     *         (in case of an error).
     *
     * Example:
     *
     *   speakapApi.get("/networks/" + networkId + "/timeline/" +
     *                  "?embed=messages.author", function(error, result) {
     *       if (error) {
     *           // handle error
     *       } else {
     *           // do something with result
     *       }
     *   });
     */
    get: function(path, callback) {

        this._request("GET", path, null, callback);
    },

    /**
     * Performs a POST request to the Speakap API.
     *
     * @param path The path of the REST endpoint, including optional query parameters.
     * @param data Object representing the JSON object to submit.
     * @param callback Callback that receives the result of the request. It received two parameters:
     *                 error - Error object with code and message properties if the request failed.
     *                 result - Parsed JSON response.
     *
     * @return A tuple containing the parsed JSON reply (in case of success) and an error object
     *         (in case of an error).
     *
     * Note that if you want to make a POST request to an action (generally all REST endpoints
     * without trailing slash), you should use the postAction() method instead, as this will use
     * the proper formatting for the POST data.
     *
     * Example:
     *
     *   speakapApi.post("/networks/" + networkId + "/messages/", {
     *       "body": "test 123",
     *       "messageType": "update",
     *       "recipient": { "type": "network", "EID": networkId }
     *   }, function(error, result) {
     *       if (error) {
     *           // handle error
     *       } else {
     *           // do something with result
     *       }
     *   });
     */
    post: function(path, data, callback) {

        this._request("POST", path, JSON.stringify(data), "application/json", callback);
    },

    /**
     * Performs a POST request to an action endpoint in the Speakap API.
     *
     * @param path The path of the REST endpoint, including optional query parameters.
     * @param data Optional object containing the form parameters to submit.
     * @param callback Callback that receives the result of the request. It received two parameters:
     *                 error - Error object with code and message properties if the request failed.
     *                 result - Parsed JSON response.
     *
     * @return A tuple containing the parsed JSON reply (in case of success) and an error object
     *         (in case of an error).
     *
     * Example:
     *
     *   speakapApi.postAction("/networks/" + networkId +
     *                         "/messages/" + messageId + "/markread", function(error, result) {
     *       if (error) {
     *           // handle error
     *       } else {
     *           // do something with result
     *       }
     *   });
     */
    postAction: function(path, data, callback) {

        if (data) {
            var params = _.map(data, function(value, key) {
                params.push(encodeURIComponent(key) + "=" + encodeURIComponent(value));
            });
            data = params.join("&");
        }

        this._request("POST", path, data, "application/x-www-form-urlencoded", callback);
    },

    /**
     * Performs a PUT request to the Speakap API.
     *
     * @param path The path of the REST endpoint, including optional query parameters.
     * @param data Object representing the JSON object to submit.
     * @param callback Callback that receives the result of the request. It received two parameters:
     *                 error - Error object with code and message properties if the request failed.
     *                 result - Parsed JSON response.
     *
     * @return A tuple containing the parsed JSON reply (in case of success) and an error object
     *         (in case of an error).
     *
     * Example:
     *
     *   speakapApi.get("/networks/" + networkId + "/timeline/" +
     *                  "?embed=messages.author", function(error, result) {
     *       if (error) {
     *           // handle error
     *       } else {
     *           // do something with result
     *       }
     *   });
     */
    put: function(path, data, callback) {

        this._request("PUT", path, JSON.stringify(data), "application/json", callback);
    },

    _request: function(method, path, data, contentType, callback) {

        var buffer;
        var headers = {
            "Authorization": "Bearer " + this.accessToken,
            "Accept": "application/vnd.speakap.api-v1.0.10+json"
        };

        if (data) {
            buffer = new Buffer(data);
            headers["Content-length"] = buffer.length;
            headers["Content-type"] = contentType + "; charset=utf-8";
        } else {
            callback = contentType;
        }

        var req = this.scheme.request({
            headers: headers,
            hostname: this.hostname,
            method: method,
            path: path
        }, function(res) {
            var responseBody = "";
            res.setEncoding("utf8");
            res.on("data", function(chunk) { responseBody += chunk; });
            res.on("end", function() {
                try {
                    if (res.statusCode === 204) {
                        callback(null, true);
                    } else if (res.statusCode >= 200 && res.statusCode < 300) {
                        var result = JSON.parse(responseBody);
                        callback(null, result);
                    } else {
                        var error = JSON.parse(responseBody);
                        callback(error);
                    }
                } catch(exception) {
                    callback({
                        code: -1001,
                        message: "Unexpected Reply",
                        description: responseBody
                    }, responseBody);
                }
            });
        });
        req.on("error", function(error) {
            callback({ code: -1000, message: "Request Failed", requestError: error });
        });

        if (buffer) {
            req.write(buffer);
        }
        req.end();
    }

});

module.exports = {
    percentEncode: percentEncode,
    signedRequest: signedRequest,
    validateSignature: validateSignature,
    API: API
};
