# Clínica - arquitectura distribuida segura

Trabajo Autónomo de Aplicaciones Distribuidas. Contiene Pacientes, Historial Clínico, API Gateway, RabbitMQ y `OAuthJWT.Api`; SQL Server es la infraestructura de persistencia.

## Servicios

- `OAuthJWT.Api`: autentica usuarios y emite los JWT con issuer `Clinica.OAuthJWT`.
- `Pacientes.Api`: CRUD protegido de pacientes y publicación de eventos `paciente.*`.
- `HistorialClinico.Api`: CRUD protegido de historiales y consumo de eventos de pacientes.
- `ApiGateway`: punto público único y reverse proxy para OAuth, Pacientes e Historial.
- `rabbitmq`: broker de eventos en el exchange `clinica.events`.

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

Requiere Docker Desktop. Copie `.env.example` como `.env` (no se versiona) y complete valores seguros:

```dotenv
JWT_KEY=<JWT_KEY_LOCAL_DE_MINIMO_32_CARACTERES>
SQL_SA_PASSWORD=<CONTRASENA_SQL_LOCAL_SEGURA>
RABBITMQ_USER=<USUARIO_RABBITMQ_LOCAL>
RABBITMQ_PASSWORD=<CONTRASENA_RABBITMQ_LOCAL_SEGURA>
OAUTH_USER=<USUARIO_OAUTH_LOCAL>
OAUTH_USER_PASSWORD=<CONTRASENA_USUARIO_OAUTH_LOCAL_SEGURA>
OAUTH_ADMIN=<USUARIO_ADMIN_LOCAL>
OAUTH_ADMIN_PASSWORD=<CONTRASENA_ADMIN_OAUTH_LOCAL_SEGURA>
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
  -Body '{"usuario":"<OAUTH_USER>","contrasena":"<OAUTH_USER_PASSWORD>"}'
$headers = @{ Authorization = "Bearer $($login.token)" }

# Sin token: 401. Con token: 200.
Invoke-WebRequest http://localhost:5100/historiales
Invoke-RestMethod http://localhost:5100/historiales -Headers $headers
```

Use las credenciales definidas en su `.env` local. No publique usuarios, contraseñas, JWT keys ni cadenas de conexión.

## Demostración para entrega

Antes de grabar, confirme que todos los contenedores estén activos y abra estas pestañas (no muestran secretos):

```powershell
docker compose ps
Start-Process http://localhost:5100
Start-Process http://localhost:5101/swagger
Start-Process http://localhost:5102/swagger
Start-Process http://localhost:5103/swagger
Start-Process http://localhost:15672
```

En RabbitMQ Management ingrese únicamente con las credenciales locales de `.env`; no las muestre en la grabación. La secuencia sugerida, con comandos reutilizables, está en [GUIÓN_VIDEO_DEMO.md](C:/Users/jordy/source/repos/MicroHolder/Microservicios/entregables/GUIÓN_VIDEO_DEMO.md) y en la [memoria de comandos](C:/Users/jordy/source/repos/MicroHolder/Microservicios/entregables/azure/MEMORIA_COMANDOS_AZURE.md).

Para obtener un JWT durante la demo:

```powershell
$login = Invoke-RestMethod -Method Post http://localhost:5100/oauth/token `
  -ContentType 'application/json' `
  -Body (@{ usuario = '<OAUTH_USER>'; contrasena = '<OAUTH_USER_PASSWORD>' } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.token)" }
```

Después, ejecute los `GET` protegidos con `$headers` o autorice en Swagger con `Bearer <token>`. Cree un paciente antes de crear un historial, y muestre en RabbitMQ la cola o los intercambios asociados a `clinica.events` para evidenciar la mensajería asíncrona.

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

Gateway público: https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io

| Servicio | URL pública / estado |
|---|---|
| Gateway | `https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io` |
| Health Gateway | `https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io/health` |
| Swagger OAuthJWT | `https://oauth-jwt-api.happypond-6af05007.australiaeast.azurecontainerapps.io/swagger` |
| Swagger Pacientes | `https://pacientes-api.happypond-6af05007.australiaeast.azurecontainerapps.io/swagger` |
| Swagger Historial | `https://historial-clinico-api.happypond-6af05007.australiaeast.azurecontainerapps.io/swagger` (puede requerir unos minutos de propagación) |

Las rutas públicas consumibles están documentadas en `entregables/azure/RUTAS_Y_CREDENCIALES_AZURE.txt`. Las credenciales reales solo deben existir en el archivo privado ignorado por Git y en secretos de Azure Container Apps.

En Azure, OAuthJWT, Pacientes e Historial usan referencias a secretos de Container Apps. Las APIs de negocio usan el ingreso TCP interno de RabbitMQ en el puerto 5672; el Gateway usa los FQDN HTTPS públicos de las Container Apps como destinos.

### Rutas públicas de Azure

| Componente | Ruta pública |
|---|---|
| Gateway health | `https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io/health` |
| Gateway: token | `POST https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io/oauth/token` |
| Gateway: pacientes | `https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io/pacientes` |
| Gateway: historiales | `https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io/historiales` |
| Gateway: eventos de pacientes | `https://api-gateway.happypond-6af05007.australiaeast.azurecontainerapps.io/historiales/eventos-pacientes` |
| OAuthJWT health | `https://oauth-jwt-api.happypond-6af05007.australiaeast.azurecontainerapps.io/health` |
| OAuthJWT token directo | `POST https://oauth-jwt-api.happypond-6af05007.australiaeast.azurecontainerapps.io/api/oauth/token` |
| Swagger OAuthJWT | `https://oauth-jwt-api.happypond-6af05007.australiaeast.azurecontainerapps.io/swagger` |
| Pacientes health | `https://pacientes-api.happypond-6af05007.australiaeast.azurecontainerapps.io/health` |
| Pacientes API directa | `https://pacientes-api.happypond-6af05007.australiaeast.azurecontainerapps.io/api/pacientes` |
| Swagger Pacientes | `https://pacientes-api.happypond-6af05007.australiaeast.azurecontainerapps.io/swagger` |
| Historial health | `https://historial-clinico-api.happypond-6af05007.australiaeast.azurecontainerapps.io/health` |
| Historial API directa | `https://historial-clinico-api.happypond-6af05007.australiaeast.azurecontainerapps.io/api/historiales` |
| Swagger Historial | `https://historial-clinico-api.happypond-6af05007.australiaeast.azurecontainerapps.io/swagger` |

RabbitMQ no se expone públicamente: usa ingreso TCP interno de Azure Container Apps en el puerto 5672.

Para la grabación en Azure, muestre las Container Apps y las URLs públicas de Gateway y Swagger. RabbitMQ debe explicarse como infraestructura interna: Pacientes e Historial se conectan a su FQDN TCP interno, por lo que no existe una URL pública de Management que deba mostrarse.

### Publicación desde GitHub Actions

El flujo `.github/workflows/publish-acr.yml` construye las cuatro imágenes, las publica en ACR y despliega las revisiones de Pacientes e Historial. Antes de ejecutarlo, configure en GitHub Actions los secretos (sin versionar valores):

- `ACR_LOGIN_SERVER`: servidor de inicio de sesión del ACR.
- `AZURE_CREDENTIALS`: credenciales JSON de un principal de servicio con permisos para publicar en ACR y actualizar las Container Apps del Resource Group.

Cada ejecución publica una etiqueta corta del commit y actualiza Pacientes e Historial con esa etiqueta, evitando depender de `latest`.

Las plantillas de credenciales y memoria de comandos están en [entregables/azure](C:/Users/jordy/source/repos/MicroHolder/Microservicios/entregables/azure). Después de la revisión elimine recursos:

```powershell
az group delete --name <NOMBRE_RESOURCE_GROUP> --yes --no-wait
```
