%dw 2.0
output application/java
---
{
  customerId: payload.customerId default uuid(),
  firstName: payload.firstName,
  lastName: payload.lastName,
  email: payload.email,
  phone: payload.phone
}