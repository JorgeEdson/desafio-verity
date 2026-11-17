CREATE USER lancamentos WITH PASSWORD 'lancamentos';
CREATE DATABASE lancamentos_db OWNER lancamentos;

CREATE USER consolidado WITH PASSWORD 'consolidado';
CREATE DATABASE consolidado_db OWNER consolidado;

REVOKE CONNECT ON DATABASE lancamentos_db FROM PUBLIC;
REVOKE CONNECT ON DATABASE consolidado_db FROM PUBLIC;
GRANT CONNECT ON DATABASE lancamentos_db TO lancamentos;
GRANT CONNECT ON DATABASE consolidado_db TO consolidado;
