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
