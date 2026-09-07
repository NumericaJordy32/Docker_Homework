/* Ejecutar en la base ClinicaDB de Azure SQL. */
IF OBJECT_ID('dbo.Paciente', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Paciente
    (
        IdPaciente INT IDENTITY(1,1) PRIMARY KEY,
        Cedula VARCHAR(20) NOT NULL UNIQUE,
        Nombre VARCHAR(100) NOT NULL,
        Apellido VARCHAR(100) NOT NULL,
        Direccion VARCHAR(250) NULL
    );
END;
GO

IF OBJECT_ID('dbo.HistorialClinico', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.HistorialClinico
    (
        IdHistorialClinico INT IDENTITY(1,1) PRIMARY KEY,
        IdPaciente INT NOT NULL,
        NumHistoria VARCHAR(30) NOT NULL UNIQUE,
        Diagnostico VARCHAR(500) NOT NULL,
        Tratamiento VARCHAR(500) NULL,
        Fecha DATETIME2 NOT NULL DEFAULT GETDATE()
    );
END;
GO
