# Installation
Add the reference assembly Speakap.SDK.dll into your project

# How to use the SDK
To use the .NET SDK, make sure you add the reference assembly Speakap.SDK.dll, and then include the namespace:
```cs
using Speakap.SDK
```
, and then use it as:
```cs
try
{
    var speakap = new Speakap.SDK.SpeakapAPI("https", "api.speakap.io", MY_APP_ID, MY_APP_SECRET);

    var path = string.Format("/networks/{0}/timeline/embed=messages.author", network_eid);

    var response = speakap.Get(path);

    // ... do something with response, it will be the JSON object in string format ...
}
catch(SpeakapApplicationException ex)
{
    // ... do something with error ...
}

```
or, to validate the signature:
```cs
try
{
    var parameters = System.Web.HttpContext.Current.Request.Form.ToDictionary();

    var signedRequest = new SignedRequest(MY_APP_SECRET);

    signedRequest.ValidateSignature(parameters);
}
catch(SpeakapSignatureValidationException ex)
{
    // ... do something with error ...
}
```
The ```ToDictionary()``` is an extension method of NameValueCollection as shown below:
```cs
public static class NameValueCollectionExtensions
{
    public static IDictionary<string, string> ToDictionary(this NameValueCollection collection)
    {
        if (collection == null || collection.AllKeys == null)
            throw new ArgumentNullException("collection");

        return collection.AllKeys.ToDictionary(key => key, v => collection[v]);
    }
}
```

