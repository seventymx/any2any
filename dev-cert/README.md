## Development Certificates

### Installation

**Note:** Many browsers manage their own certificate stores, so you may need to install the Root CA certificate in your browser to trust the development certificates.

#### Install the Root CA Certificate (Ubuntu)

**1. Copy Certificate to Trusted Store Location**

```pwsh
sudo cp rootCA.pem /usr/local/share/ca-certificates/rootCA.crt
```

**2. Update Trusted Certificates**

```pwsh
sudo update-ca-certificates
```

#### Chromium (or Chrome):

1.  Open Chromium and go to chrome://settings/security.
2.  Scroll down and click Manage certificates.
3.  Go to the Authorities tab.
4.  Click Import and select your rootCA.pem file.
5.  Ensure that the options to Trust this certificate for identifying websites are checked.

#### LibreWolf (or Firefox):

1.  Open LibreWolf and go to about:preferences#privacy.
2.  Scroll down to the Certificates section and click View Certificates.
3.  Go to the Authorities tab and click Import.
4.  Select your rootCA.pem and choose to Trust this CA to identify websites.

### Steps to create a self-signed certificate for localhost

#### 1. **Create a Root CA**

The Root CA will be trusted by your system and browser, and it will sign the development certificates (like `localhost.crt`).

**a. Generate a private key for the Root CA:**

```pwsh
openssl genrsa -out rootCA.key 2048
```

**b. Create a Root CA certificate:**

```pwsh
openssl req -x509 -new -nodes -key rootCA.key -sha256 -days 1024 -out rootCA.pem -config rootCA.conf
```

**Note:** During this process, you’ll be asked to provide information. Make sure to set a **Common Name (CN)** that reflects the purpose of the certificate, such as "Local Dev Root CA".

#### 2. **Create a Certificate for Localhost**

Now, you'll create a certificate for `localhost` signed by the Root CA.

**a. Generate a private key for `localhost`:**

```pwsh
openssl genrsa -out localhost.key 2048
```

**b. Create a certificate signing request (CSR) for `localhost`:**

```pwsh
openssl req -new -key localhost.key -out localhost.csr -config localhost.conf
```

**c. Sign the `localhost` certificate with the Root CA:**

```pwsh
openssl x509 -req -in localhost.csr -CA rootCA.pem -CAkey rootCA.key -CAcreateserial -out localhost.crt -days 500 -sha256 -extfile localhost.conf -extensions req_ext
```

This signs the certificate with the Root CA, which allows it to be trusted as long as the Root CA is trusted.

**d. Create a `.pfx` file for use in development:**

```pwsh
openssl pkcs12 -export -out localhost.pfx -inkey localhost.key -in localhost.crt -certfile rootCA.pem
```

Now you can install the Root CA certificate (`rootCA.pem`) as a trusted certificate authority on your system and browsers.
