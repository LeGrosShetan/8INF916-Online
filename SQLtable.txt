-- AVANT D'EXECUTER CE CODE, QUELQUES ETAPES :
-- - CREER UN USER "test" avec pour password "password"
-- - CREER LA DB "my_api_rest"
-- - DONNER LES TOUTES LES PERMS A test (GRANT ALL PRIVILEGES ON DATABASE my_api_rest TO test)
-- - EN ETANT CONNECTE A my_api_rest, DONNER LES PERMS A TEST SUR LE SCHEMA public (GRANT ALL PRIVILEGES ON SCHEMA public TO test)
-- - EXECUTER CE SCRIPT

CREATE EXTENSION IF NOT EXISTS "uuid-ossp";

CREATE SEQUENCE role_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;
	
CREATE SEQUENCE achievements_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

-- Table: public."Roles"

-- DROP TABLE IF EXISTS public."Roles";

CREATE TABLE IF NOT EXISTS public."Roles"
(
    "Id" integer NOT NULL DEFAULT nextval('role_id_seq'::regclass),
    "Name" character varying(255) COLLATE pg_catalog."default" NOT NULL,
    CONSTRAINT role_pkey PRIMARY KEY ("Id"),
    CONSTRAINT role_name_key UNIQUE ("Name")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Roles"
    OWNER to test;
	
-----------------------------------------

-- Table: public.Users

-- DROP TABLE IF EXISTS public."Users";

CREATE TABLE IF NOT EXISTS public."Users"
(
    "Id" uuid NOT NULL DEFAULT uuid_generate_v4(),
    "Username" character varying(255) COLLATE pg_catalog."default" NOT NULL,
    "Email" character varying(255) COLLATE pg_catalog."default" NOT NULL,
    "Password" character varying(255) COLLATE pg_catalog."default" NOT NULL,
    "Salt" character varying(255) COLLATE pg_catalog."default" NOT NULL,
    "RoleId" integer,
    CONSTRAINT "User_pkey" PRIMARY KEY ("Id"),
    CONSTRAINT "User_email_key" UNIQUE ("Email"),
    CONSTRAINT "User_username_key" UNIQUE ("Username"),
    CONSTRAINT "User_role_id_fkey" FOREIGN KEY ("RoleId")
        REFERENCES public."Roles" ("Id") MATCH SIMPLE
        ON UPDATE NO ACTION
        ON DELETE NO ACTION
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Users"
    OWNER to test;
	
-----------------------------------------

-- Table: public.Achievements

-- DROP TABLE IF EXISTS public."Achievements";
	
CREATE TABLE IF NOT EXISTS public."Achievements"
(
	"Id" integer NOT NULL DEFAULT nextval('achievements_id_seq'::regclass),
	"Name" text NOT NULL,
	"Description" text NOT NULL,
	"Image" text NOT NULL,
	CONSTRAINT "Achievement_pkey" PRIMARY KEY ("Id"),
	CONSTRAINT "Achievement_name_key" UNIQUE ("Name")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Achievements"
    OWNER to test;
	
-----------------------------------------

-- Table: public.AchievementsUsers

-- DROP TABLE IF EXISTS public."AchievementsUsers";

CREATE TABLE IF NOT EXISTS public."AchievementsUsers"
(
	"UserId" uuid,
	"AchievementId" integer,
	CONSTRAINT "UsersAchievements_pkey" PRIMARY KEY ("UserId","AchievementId"),
	CONSTRAINT "UsersAchievements_user_id_fkey" FOREIGN KEY ("UserId")
        REFERENCES public."Users"("Id") MATCH SIMPLE
        ON DELETE CASCADE,
	CONSTRAINT "UsersAchievements_achievement_id_fkey" FOREIGN KEY ("AchievementId")
        REFERENCES public."Achievements"("Id") MATCH SIMPLE
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."AchievementsUsers"
    OWNER to test;
    
-----------------------------------------

-- Table: public.Ranks

-- DROP TABLE IF EXISTS public."Ranks";

CREATE TABLE IF NOT EXISTS public."Ranks"
(
	"Id" integer NOT NULL,
	"Title" text NOT NULL,
	CONSTRAINT "Ranks_pkey" PRIMARY KEY ("Id")
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."Ranks"
    OWNER to test;
    
-----------------------------------------

-- Table: public.UsersRanks

-- DROP TABLE IF EXISTS public."UsersRanks";

CREATE TABLE IF NOT EXISTS public."UsersRanks"
(
	"UserId" uuid,
	"RankId" integer,
	CONSTRAINT "UsersRanks_pkey" PRIMARY KEY ("UserId"),
	CONSTRAINT "UsersRanks_user_id_fkey" FOREIGN KEY ("UserId")
        REFERENCES public."Users"("Id") MATCH SIMPLE
        ON DELETE CASCADE,
	CONSTRAINT "UsersRanks_rank_id_fkey" FOREIGN KEY ("RankId")
        REFERENCES public."Ranks"("Id") MATCH SIMPLE
        ON DELETE CASCADE
)

TABLESPACE pg_default;

ALTER TABLE IF EXISTS public."UsersRanks"
    OWNER to test;
	
-----------------------------------------

INSERT INTO public."Roles"(
	"Name")
	VALUES ('Player'), ('Dedicated Game Server');
	
INSERT INTO public."Achievements"(
	"Name", "Description", "Image")
	VALUES ('Hello world !', 'Join a server for the first time', 'Placeholder'), ('Redrum !', 'Kill another player for the first time', 'Placeholder');
	
INSERT INTO public."Ranks"("Id","Title") VALUES (1,'Bronze'),(2,'Argent'),(3,'Or'),(4,'Platine'),(5,'Diamant')	