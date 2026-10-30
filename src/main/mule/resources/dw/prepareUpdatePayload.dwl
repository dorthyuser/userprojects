%dw 2.0
output application/java
---
{
  customerId: attributes.uriParams.customerId,
  firstName: payload.firstName default null,
  lastName: payload.lastName default null,
  email: payload.email default null,
  phone: payload.phone default null
}