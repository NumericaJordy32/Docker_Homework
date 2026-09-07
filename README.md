# Clínica - arquitectura distribuida segura

Trabajo Autónomo de Aplicaciones Distribuidas. Contiene Pacientes, Historial Clínico, API Gateway, RabbitMQ y `OAuthJWT.Api`; SQL Server es la infraestructura de persistencia.

## Arquitectura

| Componente | Dirección o puerto |
|---|---|
| API Gateway / interfaz CRUD | http://localhost:5100 |
| API de pacientes / Swagger | http://localhost:5101/swagger |
| API de historial / Swagger | http://localhost:5102/swagger |
| RabbitMQ Management | http://localhost:15672 |
| SQL Server | `localhost,1433` |

El Gateway publica las rutas `/pacientes` y `/historiales`. Las API se comunican mediante el exchange de RabbitMQ `clinica.events`.

`Pacientes <-> RabbitMQ <-> Historial Clínico`
`Pacientes -> SQL Server PacientesDB`
`Historial Clínico -> SQL Server HistorialClinicoDB`

OAuthJWT es el único emisor de tokens. Las APIs de negocio validan JWT y requieren token para su CRUD; `DELETE` requiere el rol `Administrador`.

## Ejecución local

Requiere Docker Desktop. Cree `.env` (no se versiona):

```dotenv
JWT_KEY=UnaClaveJwtLargaYSeguraDeAlMenos32Caracteres
SQL_SA_PASSWORD=UnaClaveSqlSegura_2026!
RABBITMQ_USER=admin
RABBITMQ_PASSWORD=OtraClaveSegura_2026!
```

```powershell
docker compose up --build -d
docker compose ps
```

Cada microservicio usa una instancia SQL y volumen propios: `sqlserver-pacientes` / `PacientesDB` y `sqlserver-historial` / `HistorialClinicoDB`. Los scripts de inicialización están en [database](C:/Users/jordy/source/repos/MicroHolder/Microservicios/database).

## Prueba JWT por Gateway

```powershell
$login = Invoke-RestMethod -Method Post http://localhost:5100/oauth/token `
  -ContentType 'application/json' `
  -Body '{"usuario":"usuario","contrasena":"ClinicaUser_2026!"}'
$headers = @{ Authorization = "Bearer $($login.token)" }

# Sin token: 401. Con token: 200.
Invoke-WebRequest http://localhost:5100/historiales
Invoke-RestMethod http://localhost:5100/historiales -Headers $headers
```

Credenciales de demostración: `usuario` / `ClinicaUser_2026!`; para eliminar use `administrador` / `ClinicaAdmin_2026!`.

## Endpoints

| Servicio | URL |
|---|---|
| Gateway | `http://localhost:5100/health` |
| Token JWT | `POST http://localhost:5100/oauth/token` |
| Pacientes | `http://localhost:5100/pacientes` |
| Historial clínico | `http://localhost:5100/historiales` |
| Swagger OAuthJWT | `http://localhost:5103/swagger` |
| Swagger Pacientes | `http://localhost:5101/swagger` |
| Swagger Historial | `http://localhost:5102/swagger` |

## Azure

Complete luego del despliegue:

- Gateway: `<URL_PUBLICA_GATEWAY>`
- OAuthJWT: `<URL_PUBLICA_OAUTHJWT>`
- Pacientes: `<URL_PUBLICA_PACIENTES>`
- Historial: `<URL_PUBLICA_HISTORIAL>`

Las plantillas de credenciales y memoria de comandos están en [entregables/azure](C:/Users/jordy/source/repos/MicroHolder/Microservicios/entregables/azure). Después de la revisión elimine recursos:

```powershell
az group delete --name <NOMBRE_RESOURCE_GROUP> --yes --no-wait
```
