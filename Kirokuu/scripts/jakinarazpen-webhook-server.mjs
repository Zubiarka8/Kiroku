/**
 * Mini zerbitzaria: langilearen aplikazioak POST egiten duenean FCM jakinarazpenak bidaltzen dizkie
 * administratzaile eta zuzendari nagusiari (Turso-ren JakinarazpenTokena erabiliz).
 *
 * Inguruneak: TURSO_DATABASE_URL, TURSO_AUTH_TOKEN (edo beste izen bat eta egokitu),
 * FIREBASE_SERVICE_ACCOUNT_JSON (string osoa), WEBHOOK_GAKOA (KIROKU_TXOSTEN_JAKINARAZPEN_WEBHOOK_GAKOA berdina),
 * PORT (aukerakoa).
 *
 * Exekuzioa: npm install firebase-admin @libsql/client && node jakinarazpen-webhook-server.mjs
 */

import { createClient } from "@libsql/client";
import admin from "firebase-admin";
import http from "node:http";

const webhokGakoa = process.env.WEBHOOK_GAKOA ?? "";
const tursoUrl = process.env.TURSO_DATABASE_URL ?? "";
const tursoToken = process.env.TURSO_AUTH_TOKEN ?? "";
const serviceAccountJson = process.env.FIREBASE_SERVICE_ACCOUNT_JSON ?? "";
const port = Number(process.env.PORT ?? "8787");

if (!webhokGakoa || !tursoUrl || !tursoToken || !serviceAccountJson) {
  console.error("Falta WEBHOOK_GAKOA, TURSO_DATABASE_URL, TURSO_AUTH_TOKEN o FIREBASE_SERVICE_ACCOUNT_JSON.");
  process.exit(1);
}

const turso = createClient({ url: tursoUrl, authToken: tursoToken });
const cred = JSON.parse(serviceAccountJson);
if (!admin.apps.length) {
  admin.initializeApp({ credential: admin.credential.cert(cred) });
}

async function helburukoTokenak() {
  const emaitza = await turso.execute({
    sql: `SELECT JakinarazpenTokena FROM Erabiltzaileak
          WHERE (Rola = 1 OR Rola = 2)
            AND JakinarazpenTokena IS NOT NULL
            AND TRIM(JakinarazpenTokena) != ''`,
    args: [],
  });
  return emaitza.rows.map((r) => String(r.JakinarazpenTokena ?? "")).filter(Boolean);
}

const zerbitzaria = http.createServer(async (req, res) => {
  if (req.method !== "POST" || req.url !== "/") {
    res.writeHead(404);
    res.end();
    return;
  }

  let gorputza = "";
  for await (const zati of req) gorputza += zati;

  let datuak;
  try {
    datuak = JSON.parse(gorputza);
  } catch {
    res.writeHead(400);
    res.end();
    return;
  }

  if (datuak.webhookGakoa !== webhokGakoa) {
    res.writeHead(401);
    res.end();
    return;
  }

  const tokenak = await helburukoTokenak();
  if (!tokenak.length) {
    res.writeHead(200, { "Content-Type": "application/json" });
    res.end(JSON.stringify({ bidali: 0, arrazoia: "tokenik ez" }));
    return;
  }

  const izenburua = "Txosten berria";
  const gorputzaTestua =
    typeof datuak.langileIzenOsoa === "string"
      ? `${datuak.langileIzenOsoa}: ${datuak.deskribapena ?? ""}`
      : String(datuak.deskribapena ?? "");

  let bidali = 0;
  for (const t of tokenak) {
    try {
      await admin.messaging().send({
        token: t,
        notification: { title: izenburua, body: gorputzaTestua.slice(0, 200) },
        android: {
          notification: {
            channelId: "kiroku_txostenak",
            sound: "deep_confident",
          },
        },
        data: {
          ekintza: "txosten_berria",
          langileErabiltzaileId: String(datuak.langileErabiltzaileId ?? ""),
        },
      });
      bidali++;
    } catch (e) {
      console.error("FCM errorea:", e.message);
    }
  }

  res.writeHead(200, { "Content-Type": "application/json" });
  res.end(JSON.stringify({ bidali }));
});

zerbitzaria.listen(port, () => {
  console.log(`Jakinarazpen webhook: http://0.0.0.0:${port}/`);
});
