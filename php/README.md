# Installation
## For the tests
To run the test suite, do the following:

1. Obtain composer (see: http://getcomposer.org)
2. Run composer install
3. Run: ./bin/phpunit

## To use the SDK
To use the PHP SDK, make sure you add the right auto load path (see composer.json) and try something like:

```php
$signedRequest = new \Speakap\SDK\SignedRequest();
$signedRequest->setPayload(file_get_contents('php://input'));
if ($signedRequest->isValid()) {
    // Proceed
} else {
    // The request is invalid, let's halt gracefully
}
```