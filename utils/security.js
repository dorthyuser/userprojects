function normalizeEmail(email) { return String(email || '').trim().toLowerCase(); }
function normalizePhone(phone) { return String(phone || '').trim(); }
function maskEmail(email) { const v = normalizeEmail(email); const [u, d] = v.split('@'); if (!u || !d) return '***'; return `${u.slice(0, 2)}***@${d}`; }
function safeLog(event, data) { console.log(JSON.stringify({ event, ...data })); }
module.exports = { normalizeEmail, normalizePhone, maskEmail, safeLog };