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
