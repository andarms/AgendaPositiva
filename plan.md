# Plan: Módulo de Hospedajes

Sigue el molde del módulo **Servicios** (`Features/Admin/Servicios/`). Nada nuevo de
infraestructura: mismo patrón de feature slice, `EntidadBase`, `*DbContextExtensions`,
controller con `[Route]`, vistas con path explícito, nav en `_Layout.cshtml`.

Ubicación: `Features/Admin/Hospedajes/`

---

## Modelo de dominio

Dos tipos de lugar de hospedaje con formas de capacidad distintas → **dos entidades
separadas**, no jerarquía (los 3 campos comunes nombre/dirección/teléfono no justifican
herencia). `Dominio/`:

### Casa (`Casa.cs`)

```
Casa : EntidadBase
  Nombre                string
  Direccion             string
  Telefono              string
  NombreResponsable     string
  TelefonoResponsable   string
  CuposSolteros         int      // varones solos
  CuposSolteras         int      // mujeres solas
  CuposParejas          int      // cada pareja = 2 personas
  ResponsablePersonaId  int?     // link opcional a la Persona de la que se copiaron los datos
  Activa                bool = true
  Asignaciones          ICollection<AsignacionHospedaje>
```

Capacidad total en personas = `CuposSolteros + CuposSolteras + CuposParejas*2`.

### Hotel (`Hotel.cs`)

```
Hotel : EntidadBase
  Nombre        string
  Direccion     string
  Telefono      string
  Activo        bool = true
  Habitaciones  ICollection<HabitacionHotel>
```

### HabitacionHotel (`HabitacionHotel.cs`)

```
HabitacionHotel : EntidadBase
  HotelId          int
  Nombre           string   // "Hab. 101", etiqueta libre
  CamasSencillas   int
  CamasDobles      int
  Hotel            Hotel
  Asignaciones     ICollection<AsignacionHospedaje>
```

Dos enteros cubren **cualquier** arreglo de camas ("una sencilla, dos dobles, etc.") sin
entidad `Cama`. Capacidad de la habitación = `CamasSencillas*1 + CamasDobles*2`.

### AsignacionHospedaje (`AsignacionHospedaje.cs`)

Una fila por persona (una persona = un hospedaje), con dos FK nullables (exactamente una
seteada). Para casas se etiqueta **qué cupo** consume, porque se valida estrictamente:

```
AsignacionHospedaje : EntidadBase
  InscripcionId       int              // requerido, índice único
  CasaId              int?             // seteado si va a una casa
  HabitacionHotelId   int?             // seteado si va a un hotel
  TipoCupoCasa        TipoCupoCasa?    // Soltero | Soltera | Pareja — solo cuando CasaId != null
  Inscripcion / Casa? / HabitacionHotel?  (nav)
```

`enum TipoCupoCasa { Soltero, Soltera, Pareja }`.

Índice único en `InscripcionId`. Ocupación de un lugar = `COUNT(asignaciones)`.

**Validación estricta de cupos en casa (Q2 = sí):** al asignar a una casa según `TipoCupoCasa`:

- `Soltero`: la Persona debe ser `Genero.Masculino`; `COUNT(Soltero) < CuposSolteros`.
- `Soltera`: la Persona debe ser `Genero.Femenino`; `COUNT(Soltera) < CuposSolteras`.
- `Pareja`: `COUNT(Pareja) < CuposParejas * 2` (dos personas por cada cupo de pareja, Q3 = sí).

**Hoteles:** sin enfoque de género. Ocupación de la habitación = `COUNT(asignaciones)` vs
`CamasSencillas*1 + CamasDobles*2`; bloquear al llegar al tope.

---

## Persistencia

`HospedajesDbContextExtensions.cs` con `ConfigurarCasas`, `ConfigurarHoteles`,
`ConfigurarHabitacionesHotel`, `ConfigurarAsignacionesHospedaje` (copiar el estilo de
`ServiciosDbContextExtensions`):

- Casa/Hotel: `Nombre` `HasMaxLength(255).IsRequired()`.
- Habitación → Hotel: `OnDelete(Cascade)`.
- Asignación → Casa/Habitación: `OnDelete(SetNull)`; → Inscripción: `OnDelete(Restrict)`.
- `HasIndex(a => a.InscripcionId).IsUnique()`.

En `AppDbContext.cs`: agregar 4 `DbSet` y 4 llamadas `Configurar*()` en `OnModelCreating`.

Migración:

```
dotnet ef migrations add AgregarModuloHospedajes -p AgendaPositiva.Web
```

(Se aplica sola al arrancar — `db.Database.MigrateAsync()` en `Program.cs`.)

---

## Submódulo 1 — CRUD de casas y hoteles

`HospedajesAdminController.cs`, `[Route("admin/hospedajes")]`, `[Authorize(Roles="Administrador")]`
(confirmar si debe existir un rol `EditorDeHospedajes` como con servicios — ver decisiones).

Acciones (espejo de Servicios):

- `Index` — dashboard: lista de casas + hoteles con ocupación/capacidad.
- **Casas**: `CasaFormulario` (GET nueva/editar), `GuardarCasa` (POST), `EliminarCasa`.
- **Hoteles**: `HotelFormulario`, `GuardarHotel`, `EliminarHotel`, `HotelDetalle`
  (lista sus habitaciones).
- **Habitaciones**: `HabitacionFormulario`, `GuardarHabitacion`, `EliminarHabitacion`
  (anidadas bajo un hotel, como `Ubicaciones`/`Horarios` de un servicio).

Autollenado del responsable de casa: en `CasaFormulario`, reutilizar el patrón de búsqueda
de inscritos de `GrupoAgregarMiembros` (buscar por nombre/documento) → al elegir uno, copiar
`Persona.NombreCompleto` → NombreResponsable, `Persona.Telefono` → TelefonoResponsable, y
guardar `ResponsablePersonaId`. Dirección se escribe a mano (Persona no tiene dirección).

Vistas en `Views/` (con `_ViewStart.cshtml` apuntando al `_Layout` admin compartido):
`Index`, `CasaFormulario`, `HotelFormulario`, `HotelDetalle`, `HabitacionFormulario`.

---

## Submódulo 2 — Asignar hospedajes a inscritos

Mismo controller, rutas bajo `admin/hospedajes/asignaciones`.

- `Asignaciones` (GET) — lista inscritos con `RequiereHospedaje == true` del evento activo
  (ese flag ya existe en `Inscripcion`), mostrando su asignación actual o "sin asignar".
  Reutilizar la búsqueda por documento/edad/nombre ya existente en el listado de servicios.
  **Orden y prioridad:** los inscritos con `NecesidadesEspeciales` no vacío van **primero** y
  resaltados (badge/fila destacada); el resto ordenado por nombre. `NecesidadesEspeciales` ya
  existe en `Inscripcion` — solo hay que ordenar por él y marcarlo en la vista.
- `Asignar` (POST) — recibe InscripcionId + destino (CasaId+TipoCupoCasa o HabitacionHotelId),
  aplica la validación estricta de arriba, hace upsert en `AsignacionHospedaje`.
- `Desasignar` (POST) — borra la asignación.
- Panel lateral / selector mostrando por cada lugar: capacidad, ocupados, libres (por tipo de
  cupo en casas). Bloquear asignar si el cupo elegido está lleno o el género no coincide.

Para elegir destino: al elegir una casa, mostrar los 3 tipos de cupo con libres; al elegir un
hotel, dropdown de habitaciones con camas y libres visibles.

**Export a Excel** (`ClosedXML`, mismo patrón que Servicios): acción `ExportarExcel` con el
listado de asignaciones — persona, documento, edad, teléfono, necesidades especiales, lugar
asignado (casa/hotel + habitación/tipo de cupo), sin asignar incluidos. Necesidades especiales
como columna destacada.

---

## Navegación

En `Features/Admin/Inscripciones/Views/_Layout.cshtml`, agregar en el bloque
`User.IsInRole("Administrador")` un `<a href="/admin/hospedajes">Hospedajes</a>` junto a los
demás links del sidebar.

---

## Orden de implementación

1. Entidades + DbContext config + `DbSet`s → migración.
2. CRUD casas (con autollenado responsable).
3. CRUD hoteles + habitaciones.
4. Submódulo de asignación.
5. Link en nav.

---

## Decisiones (resueltas)

1. **Rol de acceso**: solo `Administrador` (sin `EditorDeHospedajes`). Subir de rung si se pide.
2. **Buckets de casa**: validación **estricta** — género y cupo por tipo (ver
   `AsignacionHospedaje`).
3. **Parejas**: cuentan de a dos contra `CuposParejas*2`, una fila por persona etiquetada
   `Pareja`. (No se fuerza emparejar A con B explícitamente; si más adelante se quiere ligar
   los dos miembros de la pareja, es un campo extra — no se construye hasta que se pida.)
4. **Lista de asignación**: prioriza y resalta necesidades especiales; export a Excel incluido.

Resume this session with:
claude --resume b50b6932-a532-4846-83b2-0aa6140e03be
