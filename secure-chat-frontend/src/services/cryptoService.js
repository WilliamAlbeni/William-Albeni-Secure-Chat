/**
 * CryptoService: Handles RSA Key Generation using Web Crypto API
 */

export const generateRSAKeyPair = async () => {
    // 1. Generate 2048-bit RSA-OAEP Key Pair
    const keyPair = await window.crypto.subtle.generateKey(
        {
            name: "RSA-OAEP",
            modulusLength: 2048,
            publicExponent: new Uint8Array([1, 0, 1]), // 65537
            hash: "SHA-256",
        },
        true, // extractable (required to export keys)
        ["encrypt", "decrypt"]
    );

    // 2. Export Public Key to SPKI format, then to Base64 (to send to C#)
    const exportedPublic = await window.crypto.subtle.exportKey("spki", keyPair.publicKey);
    const publicKeyBase64 = btoa(String.fromCharCode(...new Uint8Array(exportedPublic)));

    // 3. Export Private Key to PKCS8 format (to save locally)
    const exportedPrivate = await window.crypto.subtle.exportKey("pkcs8", keyPair.privateKey);
    const privateKeyBase64 = btoa(String.fromCharCode(...new Uint8Array(exportedPrivate)));

    return {
        publicKey: publicKeyBase64,
        privateKey: privateKeyBase64
    };
};