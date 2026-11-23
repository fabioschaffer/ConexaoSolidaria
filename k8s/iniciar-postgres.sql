\set ON_ERROR_STOP on
\getenv password ZABBIX_DB_PASSWORD
CREATE ROLE zabbix LOGIN PASSWORD :'password';
CREATE DATABASE zabbix OWNER zabbix;
