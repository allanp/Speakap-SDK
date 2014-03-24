<?php

namespace Speakap\SDK;

class SignedRequest
{
    /**
     * The default window a request is valid, in seconds
     */
    const DEFAULT_WINDOW = 60;

    /**
     * The default hostname to verify the request with
     */
    const DEFAULT_HOSTNAME = 'https://api.speakap.io';

    /**
     * A RFC3986 encoded string
     * @var string
     */
    private $payload;

    /**
     * The query string as elements
     * @var array
     */
    private $decodedPayload;

    /**
     * @var string
     */
    private $appId;

    /**
     * The shared secret
     * @var string
     */
    private $appSecret;

    /**
     * The default hostname to validate against
     * @var string
     */
    private $hostName;

    /**
     * The time window in seconds
     * @var int
     */
    private $signatureWindowSize;

    /**
     * @param string $appId
     * @param string $appSecret
     * @param int    $signatureWindowSize Time in seconds that the window is considered valid
     * @param string $hostName
     */
    public function __construct($appId, $appSecret, $signatureWindowSize = null, $hostName = null)
    {
        $this->appId = $appId;
        $this->appSecret = $appSecret;
        $this->signatureWindowSize = $signatureWindowSize === null ? static::DEFAULT_WINDOW : $signatureWindowSize;
        $this->hostName = $hostName === null ? static::DEFAULT_HOSTNAME : $hostName;
    }

    /**
     * @param $payload string, typically set via: file_get_contents('php://input');
     *
     * @return $this
     */
    public function setPayload($payload)
    {
        $this->payload = rawurldecode($payload);
        parse_str($this->payload, $this->decodedPayload);

        return $this;
    }

    /**
     * Whether or not the payload is valid
     *
     * @return boolean
     */
    public function isValid()
    {
        if ($this->payload !== $this->getSelfSignedRequest($this->appSecret, $this->decodedPayload)) {
            // The payload doesn't match
            return false;
        }

        if ( ! $this->isWithinWindow($this->signatureWindowSize, $this->decodedPayload)) {
            // The date of the request does not fall in the allowed window size.
            return false;
        }

        return true;
    }

    /**
     * Whether or not the request is within a sane window.
     *
     * @param integer $signatureWindowSize
     * @param array   $requestParameters
     * @return boolean
     */
    protected function isWithinWindow($signatureWindowSize, array $requestParameters)
    {
        $issuedAt = \DateTime::createFromFormat(\DATE_ISO8601, $requestParameters['issuedAt']);
        $now = new \DateTime();

        $diff = $now->getTimestamp() - $issuedAt->getTimestamp();

        // The diff must be less than, or equal to the window size. To protect against overflow possibilities
        // we test if the differences is equal to, or greater than 0.
        if ($diff <= $signatureWindowSize && $diff >= 0) {
            return true;
        }

        return false;
    }

    /**
     * Sign the remote payload with the local (shared) secret. The result should be identical to the one we got
     * from the server.
     *
     * @param string $secret
     * @param array  $requestParameters
     *
     * @return string
     */
    protected function getSelfSignedRequest($secret, array $requestParameters)
    {
        if (isset($requestParameters['signature'])) {
            unset($requestParameters['signature']);
        }

        $signature = hash_hmac('sha256', rawurlencode(http_build_query($requestParameters)), $secret, false);
        $requestParameters['signature'] = $signature;

        return http_build_query($requestParameters);
    }
}
