const express = require("express");
const app = express();

app.use(express.json());
app.use(express.static("public"));

let client = null;

app.get("/events", (req, res) => {
    res.setHeader("Content-Type", "text/event-stream");
    res.setHeader("Cache-Control", "no-cache");
    res.setHeader("Connection", "keep-alive");

    res.flushHeaders();

    client = res;

    req.on("close", () => {
        if (client === res) {
            client = null;
        }
    });
});

app.post("/speak", (req, res) => {
    const text = req.body.text;
    const volume = req.body.volume;

    const data = `data: ${JSON.stringify({ text, volume })}\n\n`;

    if (client) client.write(data);

    res.json({ status: "ok", text });
});

app.post("/pause", (req, res) => {
    const data = `data: ${JSON.stringify({ text: "pause" })}\n\n`;

    if (client) client.write(data);

    res.json({ status: "ok" });
});

app.post("/resume", (req, res) => {
    const data = `data: ${JSON.stringify({ text: "resume" })}\n\n`;

    if (client) client.write(data);

    res.json({ status: "ok" });
});

app.post("/stop", (req, res) => {
    const data = `data: ${JSON.stringify({ text: "stop" })}\n\n`;

    if (client) client.write(data);

    res.json({ status: "ok" });
});

const PORT = 3000;
app.listen(PORT, () => {
    console.log(`Server running at http://localhost:${PORT}`);
});