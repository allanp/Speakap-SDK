# Installation

1. Obtain composer
2. Update your local composer.json
3. Run composer install --no-dev

## Obtain composer
Installation happens via Composer, obtain composer (see: http://getcomposer.org)


## Update your local composer.json
Add the following two sections to your composer.json:
```javascript
    "repositories": [
        {
            "type": "vcs",
            "url": "https://github.com/SpeakapBV/Speakap-SDK.git"
        }
    ]
```

And:
```javascript
    "require": {
        "speakap/sdk" : "*"
    }
```
You can append `@dev` to the version definition if you want the latest and greatest. I you omit this definition, you get the latest stable release. For a complete example, see: https://github.com/SpeakapBV/SnakeApp/blob/master/composer.json

## Run composer install
When you updated your composer, run: `composer install --no-dev`


# How to use the SDK
To use the PHP SDK, make sure you add the right auto load path (see composer.json) and try something like:

```php
$signedRequest = new \Speakap\SDK\SignedRequest();

try {
    if ($signedRequest->validateSignature($_POST)) {
        // Proceed
    }
} catch (\Exception $e) {

    // The request is invalid, let's halt gracefully

    if ($e instanceof \Speakap\SDK\Exception\ExpiredSignatureException) {
        // The signature might be valid, but the request expired

    } else if ($e instanceof \Speakap\SDK\Exception\InvalidSignatureException) {
        // The signature is invalid. The request is probably not originating from Speakap

    }
}
```

Or you can use:

```php
$signedRequest = new \Speakap\SDK\SignedRequest();
$payload = file_get_contents('php://input', false, null, -1, 1048576); // Read up to 1 MiB
if ($signedRequest->setPayload($payload)->isValid()) {
    // Proceed
} else {
    // The request is invalid, let's halt gracefully
}
```

# To run the test suite

1. Run composer install --dev
2. cd php/
3. ./bin/phpunit

