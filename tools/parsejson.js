const Verifier = require('stream-json/utils/Verifier');
const fs = require('fs');

const verifier = new Verifier();
verifier.on('error', error => console.error(error));

const jsonStream = fs.createReadStream('../output/Marvel Future Fight.json');

jsonStream.pipe(verifier);
