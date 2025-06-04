--
-- PostgreSQL database dump
--

-- Dumped from database version 16.2
-- Dumped by pg_dump version 16.2

-- Started on 2024-05-16 07:53:06

SET statement_timeout = 0;
SET lock_timeout = 0;
SET idle_in_transaction_session_timeout = 0;
SET client_encoding = 'UTF8';
SET standard_conforming_strings = on;
SELECT pg_catalog.set_config('search_path', '', false);
SET check_function_bodies = false;
SET xmloption = content;
SET client_min_messages = warning;
SET row_security = off;

--
-- TOC entry 234 (class 1255 OID 16961)
-- Name: check_credentials(character varying, character varying); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.check_credentials(p_email character varying, p_password character varying) RETURNS boolean
    LANGUAGE plpgsql
    AS $$
DECLARE
    v_valid BOOLEAN;
BEGIN
    -- Kiểm tra xem username và password có hợp lệ không
    SELECT EXISTS (
        SELECT 1
        FROM users
        WHERE username = p_email AND password = p_password
    ) INTO v_valid;
    
    RETURN v_valid;
END;
$$;


ALTER FUNCTION public.check_credentials(p_email character varying, p_password character varying) OWNER TO postgres;

--
-- TOC entry 248 (class 1255 OID 17298)
-- Name: fc_trig_messages_afterinsert(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fc_trig_messages_afterinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	
	BEGIN	
		UPDATE "RoomMemberInfos"
		SET "FirstUnseenMessageId" = 
				CASE WHEN "FirstUnseenMessageId" IS NULL THEN NEW."Id"
					 ELSE "FirstUnseenMessageId"
				END, 
			"UnseenMessageCount" = "UnseenMessageCount" + 1,
			"LastUnseenMessageId" = NEW."Id" 
		WHERE "UserId" <> NEW."SenderId" AND "RoomId" = NEW."RoomId";	

		UPDATE "Rooms"
		SET "LastMessageId" = NEW."Id"
		WHERE "Id" = NEW."RoomId";	

		UPDATE "Rooms"
		SET "FirstMessageId" = NEW."Id"
		WHERE "Id" = NEW."RoomId" AND "FirstMessageId" IS NULL;	

		RETURN NEW;
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_afterinsert() OWNER TO postgres;

--
-- TOC entry 247 (class 1255 OID 17300)
-- Name: fc_trig_messages_afterupdate(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fc_trig_messages_afterupdate() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	BEGIN	
		UPDATE "RoomInfos"
		SET "FirstUnseenMessageId" = (
			SELECT "Id"
				FROM "Messages"
				WHERE "RoomId" = NEW."RoomId" 
					AND "SenderId" = NEW."SenderId"
					AND "IsReaded" = false
				ORDER BY "Id"
				LIMIT 1
		), 
			"UnseenMessageCount" = "UnseenMessageCount" - 1
		WHERE "MemberId" <> NEW."SenderId" AND "RoomId" = NEW."RoomId";

		UPDATE "RoomInfos"
		SET "LastUnseenMessageId" = NULL				
		WHERE "LastUnseenMessageId" = NEW."Id"
			AND "MemberId" <> NEW."SenderId" 
			AND "RoomId" = NEW."RoomId";

		RETURN NEW;
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_afterupdate() OWNER TO postgres;

--
-- TOC entry 253 (class 1255 OID 17302)
-- Name: fc_trig_messages_beforedelete(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fc_trig_messages_beforedelete() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	DECLARE
		last_unseen_message_id bigint;	
	BEGIN						
		-- sub 1 count
		UPDATE "RoomMemberInfos"
		SET "UnseenMessageCount" = "UnseenMessageCount" -1			
		WHERE "UserId" <> OLD."SenderId" AND "RoomId" = OLD."RoomId";
	
		UPDATE "Rooms"
		SET "LastMessageId" = (
				SELECT "Id" 
				FROM "Messages"
				WHERE "RoomId" = OLD."RoomId" 
					  AND "LastMessageId" <> OLD."Id"
				ORDER BY "Id" DESC
				LIMIT 1
			)
		WHERE "Id" =  OLD."RoomId" AND "LastMessageId" = OLD."Id";

		-- update FirstMessageId
		UPDATE "Rooms"
		SET "FirstMessageId" = (
			SELECT "Id" 
			FROM "Messages"
			WHERE "RoomId" = OLD."RoomId" AND "FirstMessageId" <> OLD."Id"
			ORDER BY "Id"
			LIMIT 1
		)
		WHERE "Id" =  OLD."RoomId" AND "FirstMessageId" = OLD."Id";

		WITH CTE AS (
			SELECT info."UserId", MAX(m."Id") AS "Id"
			FROM "Messages" AS m
				INNER JOIN "RoomMemberInfos" as info
					ON m."RoomId" = info."RoomId" AND m."RoomId" = OLD."RoomId"
				LEFT JOIN "MessageDetails" as md
					ON	m."Id" = md."MessageId" AND md."UserId" <> m."SenderId"
				WHERE md."MessageId" IS NULL
				GROUP BY info."UserId"			
		)

		UPDATE "RoomMemberInfos" 
		SET "LastMessageId" = CTE."Id"
		FROM CTE
		WHERE "RoomId" = OLD."RoomId";
		
		
	
		
	 	RETURN OLD;
		
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_beforedelete() OWNER TO postgres;

--
-- TOC entry 249 (class 1255 OID 17348)
-- Name: fc_trig_messages_detail_afterinsert(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fc_trig_messages_detail_afterinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	DECLARE
		room_id int;
	BEGIN
		-- get room_id
		SELECT "RoomId" INTO room_id
		FROM "Messages"
		WHERE "Id" = NEW."MessageId";
	
		-- sub 1 count
		UPDATE "RoomMemberInfos"
		SET "UnseenMessageCount" = "UnseenMessageCount" - 1			
		WHERE "UserId" = NEW."UserId" AND "RoomId" = room_id;

		-- update FirstUnseenMessageId
		UPDATE "RoomMemberInfos"
		SET "FirstUnseenMessageId" = 
			(
				SELECT m."Id"
				FROM "Messages" AS m 
					LEFT JOIN "MessageDetails" AS md 
					ON m."Id" = md."MessageId" AND md."UserId" = NEW."UserId"
				WHERE md."MessageId" is null AND m."SenderId" <> NEW."UserId" AND m."RoomId" = room_id
				ORDER BY "Id"
				LIMIT 1
			)
		WHERE "RoomId" = room_id
			AND "UserId" = NEW."UserId";
			
		-- update LastUnseenMessageId		
		UPDATE "RoomMemberInfos"
		SET "LastUnseenMessageId" = 
			(
				SELECT m."Id"
				FROM "Messages" AS m 
					LEFT JOIN "MessageDetails" AS md 
					ON m."Id" = md."MessageId" AND md."UserId" = NEW."UserId"
				WHERE md."MessageId" is null AND m."SenderId" <> NEW."UserId" AND m."RoomId" = room_id	
				ORDER BY "Id" DESC
				LIMIT 1
			)
		WHERE "RoomId" = room_id
			AND "UserId" = NEW."UserId";
	
		-- UPDATE "RoomMemberInfos"
		-- SET "LastUnseenMessageId" = NULL		
		-- WHERE "LastUnseenMessageId" = NEW."MessageId"	
		-- 	AND "UserId" = NEW."UserId"
		-- 	AND "RoomId" = room_id;
			
	 	RETURN NEW;
		
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_detail_afterinsert() OWNER TO postgres;

--
-- TOC entry 252 (class 1255 OID 17352)
-- Name: fc_trig_messages_detail_beforedelete(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fc_trig_messages_detail_beforedelete() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	DECLARE
		room_id int;
	BEGIN
		-- get room_id
		SELECT "RoomId" INTO room_id
		FROM "Messages"
		WHERE "Id" = OLD."MessageId";
					
		-- add 1 count
		UPDATE "RoomMemberInfos"
		SET "UnseenMessageCount" = "UnseenMessageCount" + 1			
		WHERE "UserId" = OLD."UserId" AND "RoomId" = room_id;

		-- update FirstUnseenMessageId
		UPDATE "RoomMemberInfos"
		SET "FirstUnseenMessageId" =
			(
				SELECT m."Id"
				FROM "Messages" AS m 
					LEFT JOIN "MessageDetails" AS md 
					ON m."Id" = md."MessageId" AND md."UserId" = OLD."UserId"
				WHERE md."MessageId" IS NULL 
				ORDER BY m."Id"
				LIMIT 1
			)
		WHERE "RoomId" = room_id
			AND "UserId" = OLD."UserId";
			
		-- update LastUnseenMessageId		
		UPDATE "RoomMemberInfos"
		SET "LastUnseenMessageId" = 
			(
				SELECT m."Id"
				FROM "Messages" AS m 
					LEFT JOIN "MessageDetails" AS md 
					ON m."Id" = md."MessageId" AND md."UserId" = OLD."UserId"
				WHERE md."MessageId" IS NULL 
				ORDER BY m."Id" DESC
				LIMIT 1
			)
		WHERE "UserId" = OLD."UserId"
			AND "RoomId" = room_id;
			
			
	 	RETURN OLD;	
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_detail_beforedelete() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- TOC entry 233 (class 1259 OID 17308)
-- Name: MessageDetails; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."MessageDetails" (
    "Id" bigint NOT NULL,
    "MessageId" bigint NOT NULL,
    "UserId" integer NOT NULL,
    "ReactionId" integer
);


ALTER TABLE public."MessageDetails" OWNER TO postgres;

--
-- TOC entry 251 (class 1255 OID 17354)
-- Name: func_message_detail_update_is_readed(bigint, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.func_message_detail_update_is_readed(message_id bigint, user_id integer) RETURNS SETOF public."MessageDetails"
    LANGUAGE plpgsql
    AS $$
	BEGIN
		RETURN QUERY 
		INSERT INTO "MessageDetails" ("MessageId", "UserId")
			VALUES (message_id, user_id)
	    RETURNING *;
									
	END;
$$;


ALTER FUNCTION public.func_message_detail_update_is_readed(message_id bigint, user_id integer) OWNER TO postgres;

--
-- TOC entry 250 (class 1255 OID 17335)
-- Name: func_message_detail_update_reaction(bigint, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.func_message_detail_update_reaction(message_id bigint, user_id integer, reaction_id integer) RETURNS SETOF public."MessageDetails"
    LANGUAGE plpgsql
    AS $$
	BEGIN
		RETURN QUERY 
		INSERT INTO "MessageDetails" ("MessageId", "UserId", "ReactionId")
			VALUES (message_id, user_id, reaction_id)
		ON CONFLICT ("MessageId", "UserId")
		DO UPDATE SET
			"ReactionId" = reaction_id RETURNING *;
									
	END;
$$;


ALTER FUNCTION public.func_message_detail_update_reaction(message_id bigint, user_id integer, reaction_id integer) OWNER TO postgres;

--
-- TOC entry 224 (class 1259 OID 16908)
-- Name: Messages; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Messages" (
    "Id" bigint NOT NULL,
    "Content" character varying NOT NULL,
    "IsImage" boolean DEFAULT false NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "SenderId" integer NOT NULL,
    "IsReaded" boolean DEFAULT false NOT NULL,
    "IsBlocked" boolean DEFAULT false,
    "RoomId" integer NOT NULL
);


ALTER TABLE public."Messages" OWNER TO postgres;

--
-- TOC entry 246 (class 1255 OID 17256)
-- Name: func_pm_update_reaction(bigint, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.func_pm_update_reaction(message_id bigint, receiver_id integer, reaction_id integer) RETURNS SETOF public."Messages"
    LANGUAGE plpgsql
    AS $$
	BEGIN
		update "Messages"
		set "ReactionId" = reaction_id
		where "Id" = message_id AND "ReceiverId" = receiver_id ;
		
		IF FOUND = true THEN
			return query SELECT * 
			fROM "Messages" 
			where "Id" = message_id
			LIMIT 1;		
		END IF;
		
	END;
$$;


ALTER FUNCTION public.func_pm_update_reaction(message_id bigint, receiver_id integer, reaction_id integer) OWNER TO postgres;

--
-- TOC entry 218 (class 1259 OID 16852)
-- Name: Blocklist; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Blocklist" (
    "Id" integer NOT NULL,
    "BlockerId" integer NOT NULL,
    "BlockedId" integer NOT NULL,
    "CreateAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public."Blocklist" OWNER TO postgres;

--
-- TOC entry 231 (class 1259 OID 17230)
-- Name: Reactions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Reactions" (
    "Id" integer NOT NULL,
    "Name" character varying(20)
);


ALTER TABLE public."Reactions" OWNER TO postgres;

--
-- TOC entry 230 (class 1259 OID 17229)
-- Name: Emotions_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Reactions" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."Emotions_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 220 (class 1259 OID 16868)
-- Name: Friendships; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Friendships" (
    "Id" integer NOT NULL,
    "SenderId" integer NOT NULL,
    "ReceiverId" integer NOT NULL,
    "IsAccepted" boolean,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public."Friendships" OWNER TO postgres;

--
-- TOC entry 222 (class 1259 OID 16885)
-- Name: Groups; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Groups" (
    "Id" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "GroupOwnerId" integer NOT NULL
);


ALTER TABLE public."Groups" OWNER TO postgres;

--
-- TOC entry 232 (class 1259 OID 17307)
-- Name: MessageDetail_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."MessageDetails" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."MessageDetail_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 229 (class 1259 OID 17208)
-- Name: RoomMemberInfos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RoomMemberInfos" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "RoomId" integer NOT NULL,
    "FirstUnseenMessageId" bigint,
    "UnseenMessageCount" bigint DEFAULT 0 NOT NULL,
    "LastUnseenMessageId" bigint,
    "canDisplayRoom" boolean DEFAULT true NOT NULL,
    "canShowNofitication" boolean DEFAULT true NOT NULL
);


ALTER TABLE public."RoomMemberInfos" OWNER TO postgres;

--
-- TOC entry 228 (class 1259 OID 17207)
-- Name: PrivateRoomInfos_Id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."RoomMemberInfos" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."PrivateRoomInfos_Id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 227 (class 1259 OID 17070)
-- Name: Rooms; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Rooms" (
    "Id" integer NOT NULL,
    "LastMessageId" bigint,
    "FirstMessageId" bigint,
    "IsGroup" boolean DEFAULT false NOT NULL,
    "Avatar" text,
    "Name" text
);


ALTER TABLE public."Rooms" OWNER TO postgres;

--
-- TOC entry 226 (class 1259 OID 17069)
-- Name: PrivateRoom_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Rooms" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public."PrivateRoom_id_seq"
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 225 (class 1259 OID 16962)
-- Name: RefreshToken; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RefreshToken" (
    "Id" uuid NOT NULL,
    "Token" character varying NOT NULL,
    "JwtId" character varying NOT NULL,
    "UserId" integer NOT NULL,
    "IsUsed" boolean DEFAULT true NOT NULL,
    "IsRevoked" boolean DEFAULT false NOT NULL,
    "IssuedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP NOT NULL,
    "ExpiredAt" timestamp with time zone NOT NULL
);


ALTER TABLE public."RefreshToken" OWNER TO postgres;

--
-- TOC entry 216 (class 1259 OID 16838)
-- Name: Users; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Users" (
    "Id" integer NOT NULL,
    "Fullname" character varying(30) NOT NULL,
    "Password" character varying(128) NOT NULL,
    "Salt" character varying(20) NOT NULL,
    "Email" character varying(40) NOT NULL,
    "Avatar" character varying,
    "IsOnline" boolean DEFAULT false NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP
);


ALTER TABLE public."Users" OWNER TO postgres;

--
-- TOC entry 217 (class 1259 OID 16851)
-- Name: blocklist_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Blocklist" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.blocklist_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 219 (class 1259 OID 16867)
-- Name: friendships_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Friendships" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.friendships_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 221 (class 1259 OID 16884)
-- Name: groups_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Groups" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.groups_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 223 (class 1259 OID 16907)
-- Name: privatemessages_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Messages" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.privatemessages_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 215 (class 1259 OID 16837)
-- Name: users_id_seq; Type: SEQUENCE; Schema: public; Owner: postgres
--

ALTER TABLE public."Users" ALTER COLUMN "Id" ADD GENERATED ALWAYS AS IDENTITY (
    SEQUENCE NAME public.users_id_seq
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1
);


--
-- TOC entry 4950 (class 0 OID 16852)
-- Dependencies: 218
-- Data for Name: Blocklist; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- TOC entry 4952 (class 0 OID 16868)
-- Dependencies: 220
-- Data for Name: Friendships; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- TOC entry 4954 (class 0 OID 16885)
-- Dependencies: 222
-- Data for Name: Groups; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Groups" OVERRIDING SYSTEM VALUE VALUES (1, 'Group1', '2024-04-21 16:09:03.287345+07', 4);
INSERT INTO public."Groups" OVERRIDING SYSTEM VALUE VALUES (2, 'Group 2', '2024-04-22 15:55:02.585112+07', 6);


--
-- TOC entry 4965 (class 0 OID 17308)
-- Dependencies: 233
-- Data for Name: MessageDetails; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (729, 2481, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (730, 2481, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (810, 2493, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (732, 2482, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (733, 2483, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (811, 2494, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (735, 2482, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (736, 2483, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (812, 2495, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (813, 2496, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (815, 2494, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (816, 2495, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (817, 2496, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (743, 2484, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2183, 2598, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1001, 2516, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1002, 2517, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1003, 2518, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1004, 2519, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1517, 2557, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1518, 2558, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1519, 2559, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (881, 2514, 5, 2);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1009, 2520, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1010, 2521, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (755, 2485, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (756, 2486, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (757, 2484, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (758, 2485, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (759, 2486, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1011, 2522, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1012, 2523, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1013, 2524, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (763, 2487, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (764, 2488, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (765, 2489, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1014, 2525, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1015, 2526, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1016, 2527, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2185, 2600, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (836, 2498, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (837, 2499, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1017, 2528, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (839, 2500, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1520, 2560, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1521, 2561, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (842, 2501, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1522, 2562, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1523, 2563, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1524, 2564, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (846, 2502, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1525, 2565, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (782, 2490, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (783, 2491, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (784, 2492, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (785, 2493, 7, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1024, 2529, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1025, 2530, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1026, 2531, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (851, 2503, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1027, 2532, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1028, 2533, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1029, 2534, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (855, 2504, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1030, 2535, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1526, 2566, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2187, 2602, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2191, 2606, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1632, 2590, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2193, 2610, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2199, 2621, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (863, 2505, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1037, 2536, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1038, 2537, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (804, 2487, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (805, 2488, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (806, 2489, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (807, 2490, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (808, 2491, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (809, 2492, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2201, 2623, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (867, 2506, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (868, 2507, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (870, 2509, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2203, 2625, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1534, 2567, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (873, 2510, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (874, 2511, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (875, 2512, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (876, 2513, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1535, 2568, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1536, 2569, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1537, 2570, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2205, 2627, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (882, 2515, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1539, 2571, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1540, 2572, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1541, 2573, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1542, 2574, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2207, 2629, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1476, 2546, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1477, 2551, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1478, 2552, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1479, 2553, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (869, 2508, 5, 1);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2327, 2646, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1648, 2593, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2212, 2632, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2213, 2633, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2329, 2648, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1826, 2596, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2341, 2650, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1457, 2540, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2175, 2612, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1459, 2541, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1460, 2538, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1461, 2539, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1462, 2542, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2177, 2615, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1464, 2543, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1973, 2608, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1467, 2544, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1471, 2545, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1571, 2575, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1480, 2554, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1481, 2555, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1482, 2556, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1573, 2576, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2350, 2652, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1576, 2577, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2362, 2654, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2364, 2656, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1489, 2550, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1491, 2549, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1493, 2548, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1495, 2547, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (814, 2497, 5, 2);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1580, 2578, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1581, 2579, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1582, 2580, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1583, 2581, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1584, 2582, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1585, 2583, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1586, 2584, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1587, 2586, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2184, 2599, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1824, 2595, 8, 1);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1640, 2591, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1641, 2592, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1622, 2585, 8, 1);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2186, 2601, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2188, 2603, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2189, 2604, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1656, 2594, 8, 2);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2190, 2605, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2192, 2609, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2303, 2617, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2304, 2618, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2305, 2619, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2198, 2614, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2200, 2622, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2202, 2624, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2204, 2626, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2206, 2628, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2211, 2631, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2306, 2620, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1829, 2597, 5, 1);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (818, 2497, 8, 1);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2307, 2630, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2308, 2635, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2309, 2636, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2310, 2637, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2311, 2638, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2220, 2634, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2312, 2639, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1623, 2587, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2313, 2640, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1625, 2588, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2314, 2641, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1628, 2589, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2323, 2643, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2324, 2644, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2325, 2645, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2328, 2647, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2330, 2649, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2342, 2651, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (1972, 2607, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2174, 2611, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2176, 2613, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2361, 2653, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2182, 2616, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2363, 2655, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2365, 2657, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2322, 2642, 5, 1);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2404, 2658, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2405, 2659, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2406, 2660, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2407, 2661, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2408, 2662, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2409, 2663, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2415, 2664, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2422, 2665, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2430, 2666, 5, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2431, 2667, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2432, 2669, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2433, 2670, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2434, 2671, 8, NULL);
INSERT INTO public."MessageDetails" OVERRIDING SYSTEM VALUE VALUES (2435, 2672, 8, NULL);


--
-- TOC entry 4956 (class 0 OID 16908)
-- Dependencies: 224
-- Data for Name: Messages; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2482, 'gffg', false, '2024-05-06 22:58:05.916773+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2483, 'g', false, '2024-05-06 22:58:06.561248+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2484, 'g', false, '2024-05-06 22:58:40.833865+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2485, 'df', false, '2024-05-06 22:59:50.102895+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2486, 'gf', false, '2024-05-06 22:59:50.91513+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2487, 'fhg', false, '2024-05-06 23:01:21.335279+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2488, 'hg', false, '2024-05-06 23:01:21.958768+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2489, 'gh', false, '2024-05-06 23:01:22.357535+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2490, 'hhg', false, '2024-05-06 23:01:29.006986+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2491, '\', false, '2024-05-06 23:01:29.681725+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2492, 'gh', false, '2024-05-06 23:01:30.093148+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2493, 'gh', false, '2024-05-06 23:01:30.337643+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2494, 'ghgh', false, '2024-05-06 23:01:37.1221+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2495, 'gf', false, '2024-05-06 23:01:37.728109+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2496, 'g', false, '2024-05-06 23:01:38.112569+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2497, 'g', false, '2024-05-06 23:01:38.462088+07', 7, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2498, 'll', false, '2024-05-06 23:35:27.825417+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2499, 'alo', false, '2024-05-07 19:01:35.545082+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2500, 'haha', false, '2024-05-07 19:01:56.784434+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2501, 'nghe', false, '2024-05-07 19:02:02.519615+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2502, 'dsgdf', false, '2024-05-07 19:02:14.04866+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2503, 'mot', false, '2024-05-07 19:02:16.991792+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2504, 'ok la', false, '2024-05-07 19:02:21.303675+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2505, 'nana na', false, '2024-05-08 00:32:03.824209+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2506, 'ai thay lag ko ?', false, '2024-05-08 00:32:25.048772+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2507, 'helo', false, '2024-05-08 00:46:33.59556+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2508, 'hnho', false, '2024-05-08 00:46:41.296297+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2509, 'sdf', false, '2024-05-08 00:47:41.204352+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2510, 'sdfsd', false, '2024-05-08 00:47:47.897245+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2511, 'nódno', false, '2024-05-08 00:47:49.150639+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2512, 'dfg', false, '2024-05-08 01:57:58.348664+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2513, 'fg', false, '2024-05-08 01:57:59.859952+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2514, 'dsfds', false, '2024-05-08 01:58:12.124744+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2515, 'dfds', false, '2024-05-08 01:58:13.428959+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2516, 'dfg', false, '2024-05-08 15:39:53.81202+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2517, 'sdf', false, '2024-05-08 15:39:53.966906+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2518, 'g', false, '2024-05-08 15:39:54.173441+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2519, 'ds', false, '2024-05-08 15:39:54.355659+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2520, 'gfd', false, '2024-05-08 15:39:54.7999+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2521, 'fg', false, '2024-05-08 15:39:55.047883+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2522, 's', false, '2024-05-08 15:39:55.257891+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2523, 'fds', false, '2024-05-08 15:39:55.618163+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2524, 'gf', false, '2024-05-08 15:39:55.8492+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2525, 'df', false, '2024-05-08 15:39:56.067524+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2526, 'gd', false, '2024-05-08 15:39:56.256963+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2527, 's', false, '2024-05-08 15:39:56.435496+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2528, 'gfd', false, '2024-05-08 15:39:56.628928+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2529, 'gfd', false, '2024-05-08 15:39:56.984498+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2530, 'sg', false, '2024-05-08 15:39:57.171766+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2531, 'fd', false, '2024-05-08 15:39:57.368919+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2532, 's', false, '2024-05-08 15:39:57.539792+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2533, 'gfd', false, '2024-05-08 15:39:57.734197+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2534, 'g', false, '2024-05-08 15:39:58.100591+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2535, 'ds', false, '2024-05-08 15:39:58.278779+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2536, 'helo', false, '2024-05-08 17:44:16.922552+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2537, 'aa', false, '2024-05-08 17:45:15.626318+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2538, 'hihi', false, '2024-05-08 21:33:11.956288+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2539, 'a', false, '2024-05-08 21:33:15.298883+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2540, 'hell', false, '2024-05-08 21:54:37.735972+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2541, 'huh ', false, '2024-05-08 21:54:48.591603+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2542, 'dsfs', false, '2024-05-08 21:56:11.262235+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2543, 'df', false, '2024-05-08 21:56:12.439023+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2544, 'aa', false, '2024-05-08 21:56:16.501461+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2545, 'df', false, '2024-05-08 21:56:18.332559+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2546, 'dg', false, '2024-05-08 21:57:18.034587+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2547, 'gh', false, '2024-05-08 22:03:43.093928+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2548, 'fg', false, '2024-05-08 22:03:43.407828+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2549, 'hfh', false, '2024-05-08 22:03:45.061985+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2550, 'gfh', false, '2024-05-08 22:03:57.037922+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2551, 'gh', false, '2024-05-08 22:03:57.437542+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2552, 'h', false, '2024-05-08 22:03:57.733384+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2553, 'h', false, '2024-05-08 22:03:57.997098+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2554, 'h', false, '2024-05-08 22:03:58.213493+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2555, 'h', false, '2024-05-08 22:03:58.420967+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2556, 'aa', false, '2024-05-08 22:04:08.205239+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2557, 'bvb', false, '2024-05-08 22:04:26.285103+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2481, 'av', false, '2024-05-06 22:56:57.059805+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2558, 'dgf', false, '2024-05-08 22:05:40.137575+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2559, 'fd', false, '2024-05-08 22:05:40.826078+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2560, 'gs', false, '2024-05-08 22:05:41.073631+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2561, 'f', false, '2024-05-08 22:05:41.298053+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2562, 'gd', false, '2024-05-08 22:05:41.481373+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2563, 's', false, '2024-05-08 22:05:41.682834+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2564, 'gf', false, '2024-05-08 22:05:41.873263+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2565, 'g', false, '2024-05-08 22:05:42.058182+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2566, 's', false, '2024-05-08 22:05:42.266237+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2567, 'gf', false, '2024-05-08 22:05:42.47768+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2568, 'd', false, '2024-05-08 22:05:42.649928+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2569, 'g', false, '2024-05-08 22:05:42.825308+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2570, 'dg', false, '2024-05-08 22:05:43.017014+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2571, 'fdgdg', false, '2024-05-08 22:10:56.02379+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2572, 'dg', false, '2024-05-08 22:10:56.457001+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2573, 'g', false, '2024-05-08 22:10:56.695165+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2574, 'gf', false, '2024-05-08 22:10:56.92688+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2575, 'lala', false, '2024-05-08 22:31:07.549956+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2576, 'hoho', false, '2024-05-08 22:31:10.751256+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2577, 'ok', false, '2024-05-08 22:31:13.157536+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2578, 'na', false, '2024-05-08 22:31:26.874321+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2579, 'fd', false, '2024-05-08 22:35:15.645434+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2580, 'hh', false, '2024-05-08 22:35:24.022351+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2581, 'g', false, '2024-05-08 22:36:14.627535+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2582, 'h', false, '2024-05-08 22:36:34.78657+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2583, 'jj', false, '2024-05-08 22:36:39.090797+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2584, 'ss', false, '2024-05-08 22:38:00.575492+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2585, 'gg', false, '2024-05-08 22:38:29.382823+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2586, 'aa', false, '2024-05-08 22:38:54.789733+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2587, 'hoa', false, '2024-05-08 22:47:08.294727+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2588, 'fs', false, '2024-05-08 22:47:10.013402+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2589, 'df', false, '2024-05-08 22:47:11.173922+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2590, 'dsfsf', false, '2024-05-08 22:47:12.247966+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2591, 'sfds', false, '2024-05-08 22:47:19.021489+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2592, 'df', false, '2024-05-08 22:47:19.98988+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2593, 'sdfds', false, '2024-05-08 22:47:24.845295+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2594, 'sdf', false, '2024-05-08 22:47:35.429711+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2595, 'asdf', false, '2024-05-09 02:07:56.398737+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2596, 'alala', false, '2024-05-09 02:08:14.84754+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2597, 'kdf', false, '2024-05-09 02:08:16.8821+07', 8, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2598, 'dfffffffffffffffffffffffffffffffffdsd dsfa ddddddddd', false, '2024-05-09 10:34:28.102801+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2599, 'jjjjjjjjjjjjjj', false, '2024-05-09 10:34:35.531568+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2600, 'dddddddddddddđ', false, '2024-05-09 10:34:38.281448+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2601, 'df', false, '2024-05-09 10:34:39.132167+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2602, 'df', false, '2024-05-09 10:34:39.766354+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2603, 'd', false, '2024-05-09 10:34:40.278495+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2604, 'd', false, '2024-05-09 10:34:40.899702+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2605, 'ddddddddđ', false, '2024-05-09 10:34:42.618314+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2606, 'ddddddddddddddddddddd', false, '2024-05-09 10:34:45.175577+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2607, 'dfg', false, '2024-05-09 10:45:06.176543+07', 6, false, false, 66);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2608, 'ggggggggggggggggg', false, '2024-05-09 10:45:07.839718+07', 6, false, false, 66);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2609, 'l;g   fdlkg nafo dfsjoifasf coasi flkask djofaisdjofiajsdkfa safksalfksdf oaidf alsdkf', false, '2024-05-09 11:20:33.524157+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2610, 'https://storage.googleapis.com/chat_app_test/c661ee0c-70ce-42e7-9935-543507e6d10a-files', true, '2024-05-09 11:32:06.893308+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2611, 'sdfs', false, '2024-05-09 20:39:57.334776+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2612, 'df', false, '2024-05-09 20:39:58.673126+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2613, 'abc', false, '2024-05-10 16:17:25.371321+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2614, 'sdf', false, '2024-05-10 16:32:15.263046+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2615, 'lala', false, '2024-05-10 17:24:55.37238+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2616, 'gdfg', false, '2024-05-10 17:25:30.748036+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2617, 'xgvxfv', false, '2024-05-10 17:55:08.30843+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2618, 'fg', false, '2024-05-10 17:55:10.244744+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2619, 'bdb', false, '2024-05-10 17:55:26.73957+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2620, 'fg', false, '2024-05-10 18:41:46.701014+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2621, 'https://storage.googleapis.com/chat_app_test/4a857616-e994-4963-af58-454393515c65-files', true, '2024-05-10 22:04:20.170646+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2622, 'lala', false, '2024-05-10 22:07:05.265093+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2623, 'https://storage.googleapis.com/chat_app_test/b0c08b97-3b2d-4e09-92a4-f3ec0dcbcbe7-files', true, '2024-05-10 22:26:34.684599+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2624, 'https://storage.googleapis.com/chat_app_test/ace361ad-0f20-4dcd-bac6-6ce2de62f6c9-files', true, '2024-05-10 22:44:06.59767+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2625, 'dfg', false, '2024-05-11 01:12:36.959215+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2626, 'https://storage.googleapis.com/chat_app_test/be57d637-cc50-4aee-a1b0-241fdb4c0772-files', true, '2024-05-11 01:12:44.616037+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2627, 'dfg', false, '2024-05-11 01:20:43.38683+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2628, 'dfg', false, '2024-05-11 01:20:44.154788+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2629, 'g', false, '2024-05-11 01:20:46.255252+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2630, 'ola', false, '2024-05-11 01:30:34.440556+07', 5, false, false, 69);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2631, 'ola', false, '2024-05-11 01:31:08.427749+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2632, 'chan', false, '2024-05-11 01:33:23.534476+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2633, 'a', false, '2024-05-11 01:48:08.526186+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2634, 'gdf', false, '2024-05-11 01:48:38.827957+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2635, 'adasdad', false, '2024-05-11 01:54:30.974393+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2636, 'asdas', false, '2024-05-11 01:54:31.776443+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2637, 'sad', false, '2024-05-11 01:54:32.246147+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2638, 'ads', false, '2024-05-11 01:54:32.669568+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2639, 's', false, '2024-05-11 01:54:32.85919+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2640, 'a', false, '2024-05-11 01:54:33.072635+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2641, 'd', false, '2024-05-11 01:54:33.252264+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2642, 'asasdad', false, '2024-05-11 01:54:42.212017+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2643, 'as', false, '2024-05-11 01:54:42.570958+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2644, 's', false, '2024-05-11 01:54:42.756595+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2645, 'h', false, '2024-05-11 01:55:18.259563+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2646, 'j', false, '2024-05-11 01:55:53.417504+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2647, 'j', false, '2024-05-11 01:55:53.63814+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2648, 'j', false, '2024-05-11 01:55:53.82113+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2649, 'j', false, '2024-05-11 01:55:53.976567+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2650, 'hk', false, '2024-05-11 01:55:58.108187+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2651, 'kl', false, '2024-05-11 01:55:58.492282+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2652, 'k', false, '2024-05-11 01:56:22.811543+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2653, 'hf', false, '2024-05-11 01:56:43.336077+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2654, 'fg', false, '2024-05-11 01:56:43.686381+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2655, 'hf', false, '2024-05-11 01:56:43.918162+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2656, 'h', false, '2024-05-11 01:56:44.111246+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2657, 'fgh', false, '2024-05-11 01:56:44.30521+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2658, 'https://storage.googleapis.com/chat_app_test/086e9119-7db9-4dcd-b73c-659bc0823d7b-files', true, '2024-05-12 02:44:17.595126+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2659, 'afsdf😛', false, '2024-05-12 03:14:58.840388+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2660, 'dfgdgdfgdfg', false, '2024-05-12 03:15:29.625383+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2661, '🫣😅', false, '2024-05-12 03:16:13.354109+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2662, '🫣😅🗽', false, '2024-05-12 03:16:24.906168+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2663, 'sdfsfsdf😘', false, '2024-05-12 03:17:55.333069+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2664, 'hello there', false, '2024-05-12 19:14:09.494015+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2665, 'Hi!', false, '2024-05-12 19:14:16.06058+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2666, 'are you ok🙂', false, '2024-05-12 19:14:40.910043+07', 8, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2667, 'sd', false, '2024-05-12 19:33:11.384718+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2668, 'sdfs', false, '2024-05-12 19:35:09.214778+07', 5, false, false, 66);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2669, 'sdfs', false, '2024-05-12 19:35:16.328918+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2670, 'dfgd', false, '2024-05-12 19:35:23.867393+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2671, 'https://storage.googleapis.com/chat_app_test/9e19edc6-e1db-4f77-9677-09685b03cbd4-files', true, '2024-05-12 19:35:35.575779+07', 5, false, false, 67);
INSERT INTO public."Messages" OVERRIDING SYSTEM VALUE VALUES (2672, 'sdfsdfsf😜🤩asdf😙\', false, '2024-05-12 20:03:52.270878+07', 5, false, false, 67);


--
-- TOC entry 4963 (class 0 OID 17230)
-- Dependencies: 231
-- Data for Name: Reactions; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Reactions" OVERRIDING SYSTEM VALUE VALUES (1, 'Like');
INSERT INTO public."Reactions" OVERRIDING SYSTEM VALUE VALUES (2, 'Hate');


--
-- TOC entry 4957 (class 0 OID 16962)
-- Dependencies: 225
-- Data for Name: RefreshToken; Type: TABLE DATA; Schema: public; Owner: postgres
--



--
-- TOC entry 4961 (class 0 OID 17208)
-- Dependencies: 229
-- Data for Name: RoomMemberInfos; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (68, 8, 69, NULL, 0, NULL, true, false);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (61, 5, 66, NULL, 0, NULL, true, true);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (108, 6, 69, NULL, 0, NULL, false, false);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (110, 7, 69, NULL, 0, NULL, false, false);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (66, 5, 69, NULL, 0, NULL, true, false);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (63, 5, 67, NULL, 0, NULL, true, false);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (62, 6, 66, 2668, 1, 2668, true, true);
INSERT INTO public."RoomMemberInfos" OVERRIDING SYSTEM VALUE VALUES (64, 8, 67, NULL, 0, NULL, true, false);


--
-- TOC entry 4959 (class 0 OID 17070)
-- Dependencies: 227
-- Data for Name: Rooms; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Rooms" OVERRIDING SYSTEM VALUE VALUES (66, 2668, 2607, false, NULL, NULL);
INSERT INTO public."Rooms" OVERRIDING SYSTEM VALUE VALUES (67, 2672, 2499, false, NULL, NULL);
INSERT INTO public."Rooms" OVERRIDING SYSTEM VALUE VALUES (69, 2630, 2481, true, NULL, 'Nhom1');


--
-- TOC entry 4948 (class 0 OID 16838)
-- Dependencies: 216
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (4, 'onetwo', 'oYYwDTMVTI5vbUraZhVi70WJAZr71Oa0d3fCxn/RA6/zKFI/wVuGrUx8Ptqf5VrUcib3Nqh8SFodQDSZ38uHXQ==', '7Sb3u56Y', 'mothai@gmail.com', NULL, false, '2024-04-20 17:09:01.50015+07');
INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (6, 'two', 'MGUUJFrj6EMYV5mwIQXht4Q56z2ciJgQucMDhJF8BaNfJDW8R7U1X7zDVSdhfg/vbyk7urqUDuAEcNMrXslppw==', 'u2m1NSxe', 'hai@gmail.com', NULL, false, '2024-04-21 16:11:10.367523+07');
INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (7, 'three', 'uUvAhheKesHSeKmp5EneNtG1UHILOq/BmgXYPbmjadp4ZFEL5QHINFFJVd3PdaHIcCwU0QwW+7GAMi2nHQUiPQ==', 'YY27qSZX', 'ba@gmail.com', NULL, false, '2024-04-22 01:35:02.422824+07');
INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (5, 'One', 'kdrKk7/wlVRjMjjSzgGvBQFAU9VPnJqPE+lBimCZmgsUB8X5uP8JJWAmxHC4+nXkcuao2VOaz8Qy3pRVKhoxPA==', 'cisD8w5h', 'mot@gmail.com', 'https://storage.googleapis.com/chat_app_test/9882777a-a585-4507-85ab-71a95fe277cc-380340.jpg', false, '2024-04-21 16:10:49.158127+07');
INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (9, 'five', 'zxo8rbcYJrYyrk5sQmYWrKDRKWlTDKKSVFnTw67G8iGen1UMh2PbKYpv1CPKP+UVmsVuJMJZxO4eV/Q1OSQDTQ==', '6QoUbsOA', 'nam@gmail.com', 'https://storage.googleapis.com/chat_app_test/301e4df4-9f17-422a-af0b-22f853b927bc-cat.jpg', false, '2024-05-10 21:57:13.516277+07');
INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (10, 'six', '8yVyXRrXrRtYI1jgkxzif6Ne48aBoUIdxNnGZMLu+Ft8g9uSOuEb5YALB/ooddQzhtrI24K2FnRpUcSULsrs9A==', 'efqNvnhX', 'sau@gmail.com', 'https://storage.googleapis.com/chat_app_test/be3c19ff-9887-418f-b5a2-338e8a2f7081-380340.jpg', false, '2024-05-12 19:01:41.990526+07');
INSERT INTO public."Users" OVERRIDING SYSTEM VALUE VALUES (8, 'four', 'UNjNKs/fcT9VPnWMeGGu81FKu/HysjrMuVxCrVWkXbP+efwisKRgz9FOCj8XTfSRS6femXucShYsc1qPcs75Xw==', 'uDooJelM', 'bon@gmail.com', 'https://storage.googleapis.com/chat_app_test/d8bc552d-4b80-411e-a59f-2bb409836cc6-cat.jpg', false, '2024-04-30 16:51:07.450137+07');


--
-- TOC entry 4971 (class 0 OID 0)
-- Dependencies: 230
-- Name: Emotions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Emotions_Id_seq"', 2, true);


--
-- TOC entry 4972 (class 0 OID 0)
-- Dependencies: 232
-- Name: MessageDetail_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MessageDetail_Id_seq"', 2441, true);


--
-- TOC entry 4973 (class 0 OID 0)
-- Dependencies: 228
-- Name: PrivateRoomInfos_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."PrivateRoomInfos_Id_seq"', 112, true);


--
-- TOC entry 4974 (class 0 OID 0)
-- Dependencies: 226
-- Name: PrivateRoom_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."PrivateRoom_id_seq"', 69, true);


--
-- TOC entry 4975 (class 0 OID 0)
-- Dependencies: 217
-- Name: blocklist_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.blocklist_id_seq', 1, false);


--
-- TOC entry 4976 (class 0 OID 0)
-- Dependencies: 219
-- Name: friendships_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.friendships_id_seq', 1, false);


--
-- TOC entry 4977 (class 0 OID 0)
-- Dependencies: 221
-- Name: groups_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.groups_id_seq', 2, true);


--
-- TOC entry 4978 (class 0 OID 0)
-- Dependencies: 223
-- Name: privatemessages_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.privatemessages_id_seq', 2672, true);


--
-- TOC entry 4979 (class 0 OID 0)
-- Dependencies: 215
-- Name: users_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_id_seq', 10, true);


--
-- TOC entry 4778 (class 2606 OID 17234)
-- Name: Reactions Emotions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Reactions"
    ADD CONSTRAINT "Emotions_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4780 (class 2606 OID 17312)
-- Name: MessageDetails MessageDetail_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT "MessageDetail_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4774 (class 2606 OID 17213)
-- Name: RoomMemberInfos PrivateRoomInfos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT "PrivateRoomInfos_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4772 (class 2606 OID 17092)
-- Name: Rooms PrivateRoom_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Rooms"
    ADD CONSTRAINT "PrivateRoom_pkey" PRIMARY KEY ("Id");


--
-- TOC entry 4762 (class 2606 OID 16856)
-- Name: Blocklist blocklist_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Blocklist"
    ADD CONSTRAINT blocklist_pkey PRIMARY KEY ("Id");


--
-- TOC entry 4764 (class 2606 OID 16873)
-- Name: Friendships friendships_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT friendships_pkey PRIMARY KEY ("Id");


--
-- TOC entry 4766 (class 2606 OID 16890)
-- Name: Groups groups_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Groups"
    ADD CONSTRAINT groups_pkey PRIMARY KEY ("Id");


--
-- TOC entry 4768 (class 2606 OID 16916)
-- Name: Messages privatemessages_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT privatemessages_pkey PRIMARY KEY ("Id");


--
-- TOC entry 4770 (class 2606 OID 16971)
-- Name: RefreshToken refreshtoken_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshToken"
    ADD CONSTRAINT refreshtoken_pkey PRIMARY KEY ("Id");


--
-- TOC entry 4776 (class 2606 OID 17215)
-- Name: RoomMemberInfos unq_user_room; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT unq_user_room UNIQUE ("UserId", "RoomId");


--
-- TOC entry 4758 (class 2606 OID 16850)
-- Name: Users users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT users_email_key UNIQUE ("Email");


--
-- TOC entry 4760 (class 2606 OID 16846)
-- Name: Users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT users_pkey PRIMARY KEY ("Id");


--
-- TOC entry 4781 (class 1259 OID 17328)
-- Name: idx_unique_message_user; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX idx_unique_message_user ON public."MessageDetails" USING btree ("MessageId", "UserId");


--
-- TOC entry 4799 (class 2620 OID 17299)
-- Name: Messages fc_trig_messages_afterinsert; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_afterinsert AFTER INSERT ON public."Messages" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_afterinsert();


--
-- TOC entry 4800 (class 2620 OID 17301)
-- Name: Messages fc_trig_messages_afterupdate; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_afterupdate AFTER UPDATE OF "IsReaded" ON public."Messages" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_afterupdate();

ALTER TABLE public."Messages" DISABLE TRIGGER fc_trig_messages_afterupdate;


--
-- TOC entry 4801 (class 2620 OID 17303)
-- Name: Messages fc_trig_messages_beforedelete; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_beforedelete BEFORE DELETE ON public."Messages" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_beforedelete();

ALTER TABLE public."Messages" DISABLE TRIGGER fc_trig_messages_beforedelete;


--
-- TOC entry 4802 (class 2620 OID 17349)
-- Name: MessageDetails fc_trig_messages_detail_afterinsert; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_detail_afterinsert AFTER INSERT ON public."MessageDetails" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_detail_afterinsert();


--
-- TOC entry 4803 (class 2620 OID 17353)
-- Name: MessageDetails fc_trig_messages_detail_beforedelete; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_detail_beforedelete AFTER DELETE ON public."MessageDetails" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_detail_beforedelete();


--
-- TOC entry 4782 (class 2606 OID 16862)
-- Name: Blocklist fk_fs_user_blocked; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Blocklist"
    ADD CONSTRAINT fk_fs_user_blocked FOREIGN KEY ("BlockedId") REFERENCES public."Users"("Id");


--
-- TOC entry 4783 (class 2606 OID 16857)
-- Name: Blocklist fk_fs_user_blocker; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Blocklist"
    ADD CONSTRAINT fk_fs_user_blocker FOREIGN KEY ("BlockerId") REFERENCES public."Users"("Id");


--
-- TOC entry 4784 (class 2606 OID 16879)
-- Name: Friendships fk_fs_user_receiver; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT fk_fs_user_receiver FOREIGN KEY ("ReceiverId") REFERENCES public."Users"("Id");


--
-- TOC entry 4785 (class 2606 OID 16874)
-- Name: Friendships fk_fs_user_sender; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT fk_fs_user_sender FOREIGN KEY ("SenderId") REFERENCES public."Users"("Id");


--
-- TOC entry 4786 (class 2606 OID 16998)
-- Name: Groups fk_groups_users_ownerid; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Groups"
    ADD CONSTRAINT fk_groups_users_ownerid FOREIGN KEY ("GroupOwnerId") REFERENCES public."Users"("Id");


--
-- TOC entry 4796 (class 2606 OID 17337)
-- Name: MessageDetails fk_md_messages; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT fk_md_messages FOREIGN KEY ("MessageId") REFERENCES public."Messages"("Id") ON DELETE CASCADE;


--
-- TOC entry 4797 (class 2606 OID 17323)
-- Name: MessageDetails fk_md_reactions; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT fk_md_reactions FOREIGN KEY ("ReactionId") REFERENCES public."Reactions"("Id");


--
-- TOC entry 4798 (class 2606 OID 17342)
-- Name: MessageDetails fk_md_users; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT fk_md_users FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- TOC entry 4787 (class 2606 OID 17093)
-- Name: Messages fk_messages_room; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT fk_messages_room FOREIGN KEY ("RoomId") REFERENCES public."Rooms"("Id");


--
-- TOC entry 4788 (class 2606 OID 16917)
-- Name: Messages fk_messages_user_sender; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT fk_messages_user_sender FOREIGN KEY ("SenderId") REFERENCES public."Users"("Id");


--
-- TOC entry 4792 (class 2606 OID 17263)
-- Name: RoomMemberInfos fk_prinfo_messages_first_unnseen; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_messages_first_unnseen FOREIGN KEY ("FirstUnseenMessageId") REFERENCES public."Messages"("Id");


--
-- TOC entry 4793 (class 2606 OID 17283)
-- Name: RoomMemberInfos fk_prinfo_messages_last_unnseen; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_messages_last_unnseen FOREIGN KEY ("LastUnseenMessageId") REFERENCES public."Messages"("Id");


--
-- TOC entry 4794 (class 2606 OID 17216)
-- Name: RoomMemberInfos fk_prinfo_room; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_room FOREIGN KEY ("RoomId") REFERENCES public."Rooms"("Id") ON DELETE CASCADE;


--
-- TOC entry 4795 (class 2606 OID 17221)
-- Name: RoomMemberInfos fk_prinfo_user; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_user FOREIGN KEY ("UserId") REFERENCES public."Users"("Id");


--
-- TOC entry 4790 (class 2606 OID 17293)
-- Name: Rooms fk_room_message_first_message; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Rooms"
    ADD CONSTRAINT fk_room_message_first_message FOREIGN KEY ("FirstMessageId") REFERENCES public."Messages"("Id");


--
-- TOC entry 4791 (class 2606 OID 17288)
-- Name: Rooms fk_room_message_last_message; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Rooms"
    ADD CONSTRAINT fk_room_message_last_message FOREIGN KEY ("LastMessageId") REFERENCES public."Messages"("Id");


--
-- TOC entry 4789 (class 2606 OID 16972)
-- Name: RefreshToken fk_rt_user; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshToken"
    ADD CONSTRAINT fk_rt_user FOREIGN KEY ("UserId") REFERENCES public."Users"("Id");


-- Completed on 2024-05-16 07:53:06

--
-- PostgreSQL database dump complete
--

