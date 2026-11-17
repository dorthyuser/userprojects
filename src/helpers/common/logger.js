function maskEmail(email) {
  if (!email || typeof email !== "string" || !email.includes("@")) return "";
  const [name, domain] = email.split("@");
  return `${name.slice(0, 2)}***@${domain}`;
}

function safeLog(context, message, meta = {}) {
  const sanitized = { ...meta };
  if (sanitized.guestEmail)
    sanitized.guestEmail = maskEmail(sanitized.guestEmail);
  if (sanitized.guestPhone) sanitized.guestPhone = "***";
  if (sanitized.password) delete sanitized.password;
  context.log(JSON.stringify({ message, ...sanitized }));
}

module.exports = { safeLog };
