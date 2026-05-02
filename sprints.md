# DataMarket SA — Sprints Hipotéticos

> Sistema actual: ASP.NET Core 8 · PostgreSQL (Neon) · Roles: Admin / Cashier  
> Módulos: Auth, Products, Carts, Sales, Reports · Deploy: Render + Docker

---

## Sprint 1 — Seguridad & Gestión de Usuarios
**Duración estimada:** 2 semanas  
**Objetivo:** Blindar los puntos de acceso del sistema, ampliar el modelo de usuarios/roles y dejar trazabilidad completa de cada acción sensible.

### Contexto y motivación
El sistema actualmente tiene autenticación funcional (JWT + Cookie), pero presenta varias brechas que en un entorno real representan riesgos críticos: no existe límite de intentos de login, el JWT no puede revocarse antes de expirar, no hay forma de auditar quién hizo qué, y la clave secreta del JWT está hardcodeada en `appsettings.json`. Este sprint cierra esas brechas.

---

### Tareas

#### T1-01 · Bloqueo por intentos fallidos de login
- Agregar campos `LoginAttempts` (int) y `LockedUntil` (DateTime?) al modelo `User`.
- En `AuthController.Login`: incrementar el contador en cada intento fallido. Al llegar a **5 intentos**, bloquear la cuenta por **15 minutos**.
- Respuesta al intentar entrar con cuenta bloqueada: `423 Locked` con el tiempo restante.
- Resetear el contador a 0 en cada login exitoso.

#### T1-02 · Refresh Token
- Nuevo modelo `RefreshToken` (Id, Token, UserId, ExpiresAt, RevokedAt, CreatedAt).
- `POST /api/auth/refresh` — recibe el refresh token, valida que no esté revocado ni expirado, emite un nuevo JWT y rota el refresh token.
- `POST /api/auth/logout` — revoca el refresh token activo del usuario (invalida la sesión API inmediatamente).
- Los refresh tokens expiran en **7 días** y son de un solo uso (rotación en cada uso).

#### T1-03 · Cambio de contraseña
- `PUT /api/auth/password` — requiere autenticación. Body: `{ currentPassword, newPassword, confirmPassword }`.
- Validar que la contraseña actual sea correcta antes de cambiar.
- Revocar todos los refresh tokens activos del usuario al cambiar contraseña (forzar re-login en todos los dispositivos).

#### T1-04 · Rol Manager
- Agregar un tercer rol `"Manager"` entre `Admin` y `Cashier`.
- **Permisos del Manager:**
  - Puede acceder a todos los reportes (`/api/reports/*`).
  - Puede consultar y actualizar stock de productos (`PATCH /api/products/{id}/stock`).
  - No puede crear, editar ni eliminar productos.
  - No puede registrar nuevos usuarios.
  - No puede ver la lista completa de usuarios.
- Actualizar las anotaciones `[Authorize(Roles = "...")]` en los controladores afectados.
- Actualizar `AuthController.Register` para aceptar `"Manager"` como rol válido.

#### T1-05 · Gestión de usuarios (Admin)
- `GET /api/users` `[Admin]` — lista todos los usuarios con su rol y estado.
- `PUT /api/users/{id}` `[Admin]` — permite cambiar el rol y activar/desactivar la cuenta (campo `IsActive` en `User`).
- `DELETE /api/users/{id}` `[Admin]` — soft delete (desactiva la cuenta, no borra el registro para preservar historial de ventas).
- Un usuario con `IsActive = false` no puede autenticarse aunque la contraseña sea correcta.

#### T1-06 · Audit Log
- Nuevo modelo `AuditLog` (Id, UserId, Action, EntityType, EntityId, Detail, IpAddress, Timestamp).
- Registrar automáticamente los siguientes eventos:
  - `Login.Success` / `Login.Failed` / `Login.Locked`
  - `User.Created` / `User.RoleChanged` / `User.Deactivated`
  - `Product.Created` / `Product.Updated` / `Product.Deleted` / `Product.StockUpdated`
  - `Sale.Created` / `Sale.Cancelled`
- `GET /api/audit` `[Admin]` — consulta el log con filtros por fecha, usuario y tipo de acción.
- Implementar como un `IAuditService` inyectado en los controladores para no ensuciar la lógica de negocio.

#### T1-07 · Rate Limiting en endpoints de autenticación
- Integrar el middleware de rate limiting nativo de ASP.NET Core 8 (`AddRateLimiter`).
- Política para `POST /api/auth/login`: máximo **10 requests por minuto por IP**.
- Respuesta al superar el límite: `429 Too Many Requests`.
- Esto actúa como segunda capa de protección junto con el bloqueo por intentos del T1-01.

#### T1-08 · Mover secretos a variables de entorno
- Eliminar la clave JWT del `appsettings.json` (actualmente hardcodeada como `DataMarketSuperSecretKey_ChangeInProduction_Min32Chars!`).
- Leer exclusivamente desde la variable de entorno `JWT__KEY` en producción.
- Agregar validación al inicio: si `Jwt:Key` está vacío o tiene menos de 32 caracteres, la app lanza una excepción descriptiva antes de arrancar.
- Documentar las variables requeridas en un archivo `.env.example` en la raíz del repo.

---

### Definición de Terminado (DoD)
- [ ] Todos los endpoints nuevos retornan los códigos HTTP correctos y están protegidos con el rol apropiado.
- [ ] El login falla de forma segura (mismo mensaje para usuario inexistente y contraseña incorrecta, para no enumerar usuarios).
- [ ] Los secretos no están en ningún archivo commiteado al repositorio.
- [ ] El Audit Log registra al menos los eventos de login y cambios de productos.
- [ ] Los tests manuales en Swagger confirman los flujos de bloqueo y refresh.

---
---

## Sprint 2 — Crecimiento: Inventario Avanzado, Ventas Mejoradas & Analytics
**Duración estimada:** 2 semanas  
**Objetivo:** Expandir el sistema más allá del punto de venta básico hacia una plataforma de gestión comercial: control de proveedores, flujos de devolución, descuentos, alertas automáticas y reportes enriquecidos.

### Contexto y motivación
El sistema ya registra ventas y controla stock, pero el ciclo de vida de un negocio real requiere más: ¿de dónde vienen los productos? ¿qué pasa cuando una venta se cancela? ¿cómo se aplica un descuento? ¿cómo se alerta al administrador antes de quedarse sin stock? Este sprint extiende el dominio hacia esos flujos.

---

### Tareas

#### T2-01 · Modelo Supplier y Órdenes de Compra
- Nuevo modelo `Supplier` (Id, Name, ContactEmail, Phone, IsActive).
- Nuevo modelo `PurchaseOrder` (Id, SupplierId, CreatedByUserId, Status[Pending/Approved/Cancelled], CreatedAt, ApprovedAt).
- Nuevo modelo `PurchaseOrderItem` (Id, PurchaseOrderId, ProductId, Quantity, UnitCost).
- Endpoints `[Admin/Manager]`:
  - `POST /api/suppliers` · `GET /api/suppliers` · `PUT /api/suppliers/{id}`
  - `POST /api/purchase-orders` — crea la orden en estado `Pending`.
  - `PUT /api/purchase-orders/{id}/approve` `[Admin]` — aprueba la orden, incrementa el stock de cada producto en la cantidad ordenada.
  - `PUT /api/purchase-orders/{id}/cancel` `[Admin]` — cancela sin afectar el stock.
- Esto cierra el ciclo de inventario: las compras incrementan stock, las ventas lo decrementan.

#### T2-02 · Cancelación y Devolución de Ventas
- `PUT /api/sales/{id}/cancel` `[Admin/Manager]` — cambia el `Status` de la venta a `"Cancelled"` y restaura el stock de cada ítem.
- Solo se puede cancelar una venta en estado `"Completed"` y dentro de las **24 horas** siguientes a su creación.
- Si el stock de algún producto fue modificado manualmente desde la venta, registrar una advertencia en el Audit Log pero proceder igual.
- El reporte diario y semanal ya excluye ventas canceladas (`Where(s => s.Status == "Completed")`), por lo que los reportes existentes quedan correctos automáticamente.

#### T2-03 · Sistema de Descuentos
- Nuevo modelo `Discount` (Id, Code, Type[Percentage/Fixed], Value, MinPurchaseAmount, MaxUses, UsedCount, ExpiresAt, IsActive).
- Ejemplos: `PROMO10` = 10% de descuento; `DESC500` = $500 de descuento fijo con mínimo de compra de $5.000.
- `POST /api/discounts` `[Admin]` — crear un descuento.
- `GET /api/discounts/validate?code=PROMO10` `[Auth]` — verificar si un código es válido para el carrito actual.
- Modificar `CreateSaleDto` para incluir un campo opcional `DiscountCode`.
- En `SalesController.CreateSale`: aplicar el descuento al `TotalAmount`, guardar el `DiscountId` en la venta (nuevo campo en `Sale`), incrementar `UsedCount`.
- El descuento aplicado queda registrado en la venta para trazabilidad en los reportes.

#### T2-04 · Alertas de Stock Bajo por Email
- Integrar envío de emails via SMTP (compatible con Gmail / SendGrid).
- Al procesar una venta (`CreateSale`), si algún producto queda con stock igual o menor a su `LowStockThreshold`, enviar un email al Admin.
- El email incluye: nombre del producto, stock actual, umbral configurado y un enlace al reporte de inventario.
- Implementar como un `INotificationService` para desacoplar el transporte de email de la lógica de ventas.
- Configuración vía variables de entorno: `SMTP__HOST`, `SMTP__PORT`, `SMTP__USER`, `SMTP__PASS`, `SMTP__ADMIN_EMAIL`.
- Evitar envíos duplicados: si ya se envió alerta para ese producto en las últimas 6 horas, no volver a enviar.

#### T2-05 · Reporte Mensual y por Categoría
- `GET /api/reports/sales/monthly?year=2026&month=5` `[Admin/Manager]` — ventas del mes agrupadas por día, total de ingresos, total de ítems vendidos.
- `GET /api/reports/sales/by-category?from=...&to=...` `[Admin/Manager]` — ingresos y cantidad de unidades vendidas agrupados por categoría de producto en un rango de fechas.
- `GET /api/reports/products/top-selling?limit=10&from=...&to=...` `[Admin/Manager]` — top N productos más vendidos por cantidad de unidades en el período.

#### T2-06 · Dashboard en la Página Index
- Reemplazar la pantalla `Index.cshtml` actual (vacía o básica) por un dashboard con tarjetas de métricas.
- Widgets para Admin/Manager:
  - Ventas del día: cantidad y monto total.
  - Ingresos de la semana actual vs. semana anterior (variación %).
  - Top 3 productos más vendidos en los últimos 7 días.
  - Alertas de stock bajo: productos que ya están por debajo de su umbral.
- Widgets para Cashier:
  - Mis ventas del día: cuántas ventas procesó y el monto acumulado.
  - Acceso rápido al carrito activo.
- El dashboard consume los endpoints existentes de reportes via fetch JS, sin necesidad de lógica adicional en el servidor.

#### T2-07 · Exportar Reportes a CSV
- `GET /api/reports/sales/export?from=2026-05-01&to=2026-05-31` `[Admin/Manager]` — devuelve un archivo `.csv` con todas las ventas del período (Id, Fecha, Cajero, Descuento, Total, Items).
- `GET /api/reports/inventory/export` `[Admin/Manager]` — devuelve el inventario completo en `.csv` (Producto, Categoría, Stock, Umbral, Precio).
- Implementar con `CsvHelper` o manualmente con `StringBuilder`, devolviendo `File(bytes, "text/csv", "filename.csv")`.

#### T2-08 · Historial de Cambios de Precio
- Nuevo modelo `PriceHistory` (Id, ProductId, OldPrice, NewPrice, ChangedByUserId, ChangedAt).
- Al ejecutar `PUT /api/products/{id}` y el campo `Price` cambia, registrar automáticamente una fila en `PriceHistory`.
- `GET /api/products/{id}/price-history` `[Admin]` — devuelve el historial de cambios de precio del producto.
- Útil para análisis posterior y para entender el impacto de cambios de precio en el volumen de ventas.

---

### Definición de Terminado (DoD)
- [ ] El ciclo completo proveedor → orden de compra → aprobación → stock actualizado funciona end-to-end.
- [ ] Una venta puede cancelarse dentro del plazo y el stock se restaura correctamente.
- [ ] Un descuento válido reduce el `TotalAmount` y queda registrado en la venta.
- [ ] Se recibe un email de alerta al procesar una venta que lleve un producto a stock bajo.
- [ ] El dashboard muestra datos reales al hacer login como Admin y como Cashier.
- [ ] Los archivos CSV exportados abren correctamente en Excel.

---

## Resumen de Valor por Sprint

| | Sprint 1 — Seguridad | Sprint 2 — Crecimiento |
|---|---|---|
| **Riesgo mitigado** | Brute force, tokens robados, secretos expuestos | Ventas sin proveedor, sin devolución, sin alertas |
| **Nuevos roles** | Manager | — |
| **Nuevos modelos** | RefreshToken, AuditLog | Supplier, PurchaseOrder, Discount, PriceHistory |
| **Nuevos endpoints** | ~10 | ~15 |
| **Valor de negocio** | Cumplimiento mínimo de seguridad para producción real | Ciclo comercial completo: compra → venta → devolución |
