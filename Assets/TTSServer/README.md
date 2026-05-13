# TTS Server

When running the application in the editor, the browsers speech synthesizer cannot be used directly, instead this web server is used.

## Setup:
1. Install needed packages: `npm install express`
2. Start the server: `node server.js`
3. Navigate to: `http://localhost:3000/` (The port is hardcoded to be 3000 in Unity, if the port appears different, change it there.)
4. Interact with the page at least once by clicking anywhere, test that the `speak` button works
5. As long as the tab is open, unity should forward all TTS calls to the browser.