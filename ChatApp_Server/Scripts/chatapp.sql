--
-- PostgreSQL database dump
--

-- Dumped from database version 16.2
-- Dumped by pg_dump version 16.2

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
-- Name: fc_trig_messages_afterinsert(); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.fc_trig_messages_afterinsert() RETURNS trigger
    LANGUAGE plpgsql
    AS $$
	
	BEGIN			
		UPDATE "Rooms"
		SET "LastMessageId" = NEW."Id"
		WHERE "Id" = NEW."RoomId";	

		UPDATE "Rooms"
		SET "FirstMessageId" = NEW."Id"
		WHERE "Id" = NEW."RoomId" AND "FirstMessageId" IS NULL;	

		UPDATE "RoomMemberInfos"
		SET "UnseenMessageCount" = "UnseenMessageCount" + 1		
		WHERE "UserId" <> NEW."SenderId" AND "RoomId" = NEW."RoomId";

		UPDATE "RoomMemberInfos"
		SET "FirstUnseenMessageId" = NEW."Id"		
		WHERE "UserId" <> NEW."SenderId" AND "RoomId" = NEW."RoomId" AND "FirstUnseenMessageId" IS NULL;
	
		RETURN NEW;
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_afterinsert() OWNER TO postgres;

--
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
	
		-- update FirstUnseenMessageId
		UPDATE "RoomMemberInfos"
		SET "FirstUnseenMessageId" = NULL
		WHERE "UserId" <> OLD."SenderId" AND "RoomId" = OLD."RoomId";
	
	 	RETURN OLD;
		
	END;
$$;


ALTER FUNCTION public.fc_trig_messages_beforedelete() OWNER TO postgres;

SET default_tablespace = '';

SET default_table_access_method = heap;

--
-- Name: MessageDetails; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."MessageDetails" (
    "Id" bigint NOT NULL,
    "MessageId" bigint NOT NULL,
    "UserId" integer NOT NULL,
    "ReactionId" integer,
    "RoomId" integer NOT NULL
);


ALTER TABLE public."MessageDetails" OWNER TO postgres;

--
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
-- Name: func_message_detail_update_reaction(bigint, integer, integer, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.func_message_detail_update_reaction(message_id bigint, room_id integer, user_id integer, reaction_id integer) RETURNS SETOF public."MessageDetails"
    LANGUAGE plpgsql
    AS $$
	BEGIN
		RETURN QUERY 
		INSERT INTO "MessageDetails" ("MessageId", "RoomId", "UserId", "ReactionId")
			VALUES (message_id, room_id, user_id, reaction_id)
		ON CONFLICT ("MessageId", "UserId")
		DO UPDATE SET
			"ReactionId" = reaction_id RETURNING *;
									
	END;
$$;


ALTER FUNCTION public.func_message_detail_update_reaction(message_id bigint, room_id integer, user_id integer, reaction_id integer) OWNER TO postgres;

--
-- Name: RoomMemberInfos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."RoomMemberInfos" (
    "Id" integer NOT NULL,
    "UserId" integer NOT NULL,
    "RoomId" integer NOT NULL,
    "UnseenMessageCount" bigint DEFAULT 0 NOT NULL,
    "canDisplayRoom" boolean DEFAULT true NOT NULL,
    "canShowNofitication" boolean DEFAULT true NOT NULL,
    "FirstUnseenMessageId" bigint
);


ALTER TABLE public."RoomMemberInfos" OWNER TO postgres;

--
-- Name: func_rm_update_first_unseen(bigint, integer); Type: FUNCTION; Schema: public; Owner: postgres
--

CREATE FUNCTION public.func_rm_update_first_unseen(message_id bigint, user_id integer) RETURNS SETOF public."RoomMemberInfos"
    LANGUAGE plpgsql
    AS $$
DECLARE
    room_id integer;
BEGIN
    -- Get the RoomId from the message
    SELECT "RoomId" INTO room_id
    FROM "Messages"
    WHERE "Id" = message_id;

    -- Update the RoomMemberInfos and return the updated row
    RETURN QUERY
    UPDATE "RoomMemberInfos"
    SET "FirstUnseenMessageId" = (
        SELECT "Id"
        FROM "Messages"
        WHERE "RoomId" = room_id AND "SenderId" <> user_id AND "Id" > message_id
        ORDER BY "Id"
        LIMIT 1
    ),
		"UnseenMessageCount" = (
		SELECT COUNT(*)
		FROM "Messages"
		WHERE "RoomId" = room_id AND "SenderId" <> user_id AND "Id" > message_id
		)
    WHERE "RoomId" = room_id AND "UserId" = user_id
    RETURNING *;
END;
$$;


ALTER FUNCTION public.func_rm_update_first_unseen(message_id bigint, user_id integer) OWNER TO postgres;

--
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
-- Name: Reactions; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Reactions" (
    "Id" integer NOT NULL,
    "Name" character varying(20)
);


ALTER TABLE public."Reactions" OWNER TO postgres;

--
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
-- Name: GroupInfos; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."GroupInfos" (
    "GroupId" integer NOT NULL,
    "Name" character varying(100) NOT NULL,
    "Avatar" text,
    "GroupOwnerId" integer NOT NULL
);


ALTER TABLE public."GroupInfos" OWNER TO postgres;

--
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
-- Name: Messages; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Messages" (
    "Id" bigint NOT NULL,
    "Content" character varying NOT NULL,
    "IsImage" boolean DEFAULT false NOT NULL,
    "CreatedAt" timestamp with time zone DEFAULT CURRENT_TIMESTAMP,
    "SenderId" integer NOT NULL,
    "RoomId" integer NOT NULL,
    "QuoteId" bigint
);


ALTER TABLE public."Messages" OWNER TO postgres;

--
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
-- Name: Rooms; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public."Rooms" (
    "Id" integer NOT NULL,
    "LastMessageId" bigint,
    "FirstMessageId" bigint,
    "IsGroup" boolean DEFAULT false NOT NULL
);


ALTER TABLE public."Rooms" OWNER TO postgres;

--
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
-- Name: room_id; Type: TABLE; Schema: public; Owner: postgres
--

CREATE TABLE public.room_id (
    "RoomId" integer
);


ALTER TABLE public.room_id OWNER TO postgres;

--
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
-- Data for Name: Blocklist; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Blocklist" ("Id", "BlockerId", "BlockedId", "CreateAt") FROM stdin;
\.


--
-- Data for Name: Friendships; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Friendships" ("Id", "SenderId", "ReceiverId", "IsAccepted", "CreatedAt") FROM stdin;
\.


--
-- Data for Name: GroupInfos; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."GroupInfos" ("GroupId", "Name", "Avatar", "GroupOwnerId") FROM stdin;
111	Nhom2	\N	4
102	Nhom1	\N	5
\.


--
-- Data for Name: MessageDetails; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."MessageDetails" ("Id", "MessageId", "UserId", "ReactionId", "RoomId") FROM stdin;
4585	3851	5	1	102
4587	3940	5	1	98
4589	3928	5	1	98
4592	3973	8	1	98
4596	3980	5	3	98
4601	4072	5	1	98
\.


--
-- Data for Name: Messages; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Messages" ("Id", "Content", "IsImage", "CreatedAt", "SenderId", "RoomId", "QuoteId") FROM stdin;
3846	mot	f	2024-05-30 07:58:36.95498+07	5	98	\N
3848	a	f	2024-05-30 08:51:59.480799+07	5	102	\N
3849	a	f	2024-05-30 08:52:00.936714+07	5	102	\N
3850	a	f	2024-05-30 08:52:02.413778+07	5	102	\N
3851	df	f	2024-05-30 09:07:49.078537+07	8	102	\N
3852	g	f	2024-05-30 09:19:43.279413+07	5	98	\N
3853	g	f	2024-05-30 09:19:44.523079+07	5	98	\N
3854	g	f	2024-05-30 09:19:45.278009+07	5	98	\N
3855	g	f	2024-05-30 09:19:45.656397+07	5	98	\N
3856	g	f	2024-05-30 09:19:45.968866+07	5	98	\N
3857	g	f	2024-05-30 09:21:36.085417+07	5	98	\N
3858	f	f	2024-05-30 09:22:45.484845+07	5	102	\N
3859	g	f	2024-05-30 09:23:16.061252+07	5	102	\N
3860	dfg	f	2024-05-30 11:41:28.916023+07	5	98	\N
3861	f	f	2024-05-30 11:41:29.819674+07	5	98	\N
3862	dfg	f	2024-05-30 11:41:30.142003+07	5	98	\N
3863	oo	f	2024-05-30 15:06:46.622935+07	5	98	\N
3864	qq	f	2024-05-30 15:08:17.93778+07	5	98	\N
3865	tt	f	2024-05-30 15:11:06.28496+07	5	98	\N
3866	vv	f	2024-05-30 15:11:57.327772+07	5	98	\N
3867	ii	f	2024-05-30 15:12:37.309136+07	5	98	\N
3868	oo	f	2024-05-30 15:22:18.308512+07	5	98	\N
3869	aa	f	2024-05-30 15:22:35.548316+07	5	98	\N
3870	a	f	2024-05-30 15:22:54.46978+07	5	98	\N
3871	b	f	2024-05-30 15:23:17.986917+07	5	98	\N
3872	t	f	2024-05-30 15:23:55.144748+07	5	98	\N
3873	z	f	2024-05-30 15:25:57.441776+07	5	98	\N
3874	p	f	2024-05-30 15:26:53.096475+07	5	98	\N
3875	n	f	2024-05-30 15:27:18.371606+07	5	98	\N
3876	p	f	2024-05-30 15:28:04.664185+07	5	98	\N
3877	z	f	2024-05-30 15:28:06.686031+07	5	98	\N
3878	y	f	2024-05-30 15:28:24.806309+07	5	98	\N
3879	y	f	2024-05-30 15:28:30.20034+07	5	98	\N
3880	o	f	2024-05-30 15:30:00.956653+07	5	98	\N
3881	a	f	2024-05-30 15:30:51.019716+07	5	98	\N
3882	y	f	2024-05-30 15:31:43.667183+07	5	98	\N
3883	p	f	2024-05-30 15:32:41.833348+07	5	98	\N
3884	mot hai ba	f	2024-05-30 15:32:54.705453+07	5	98	\N
3885	ii	f	2024-05-30 15:38:53.825876+07	5	98	\N
3886	nn	f	2024-05-30 15:39:21.067303+07	5	98	\N
3887	oo	f	2024-05-30 15:39:23.528798+07	5	98	\N
3888	aa	f	2024-05-30 15:40:04.872127+07	5	98	\N
3889	zz	f	2024-05-30 15:42:13.202018+07	5	98	\N
3890	aa	f	2024-05-30 15:43:05.165962+07	5	98	\N
3891	xx	f	2024-05-30 15:44:01.886934+07	5	98	\N
3892	xx	f	2024-05-30 15:45:46.182949+07	5	98	\N
3893	pp	f	2024-05-30 15:45:49.35347+07	5	98	\N
3894	yy	f	2024-05-30 15:46:24.422472+07	5	98	\N
3895	la lal a	f	2024-05-30 15:58:20.6811+07	5	98	\N
3896	hihi	f	2024-05-30 15:58:41.115494+07	5	98	\N
3897	hh	f	2024-05-30 15:58:49.458994+07	5	98	\N
3898	la nhi	f	2024-05-30 15:58:58.573947+07	5	98	\N
3899	dd	f	2024-05-30 15:59:56.198136+07	5	98	\N
3900	fdg	f	2024-05-30 16:00:13.194477+07	5	98	\N
3901	a	f	2024-05-30 16:02:50.046897+07	5	98	\N
3902	sdfs	f	2024-05-30 16:03:46.37805+07	5	98	\N
3903	a	f	2024-05-30 16:04:28.185285+07	5	98	\N
3904	f	f	2024-05-30 16:07:57.692644+07	5	98	\N
3905	mot	f	2024-05-30 16:08:52.777132+07	5	98	\N
3906	hai	f	2024-05-30 16:09:49.514074+07	5	98	\N
3907	ba	f	2024-05-30 16:13:32.409009+07	5	98	\N
3908	dfg	f	2024-05-30 17:01:56.792795+07	8	98	\N
3909	df	f	2024-05-30 17:01:57.009725+07	8	98	\N
3910	f	f	2024-05-30 17:01:57.155019+07	8	98	\N
3911	g	f	2024-05-30 17:01:57.32629+07	8	98	\N
3912	ds	f	2024-05-30 17:01:57.475752+07	8	98	\N
3913	g	f	2024-05-30 17:01:57.737869+07	8	98	\N
3914	d	f	2024-05-30 17:01:57.793076+07	8	98	\N
3915	fg	f	2024-05-30 17:01:57.933325+07	8	98	\N
3916	sd	f	2024-05-30 17:01:58.099454+07	8	98	\N
3917	f	f	2024-05-30 17:01:58.283631+07	8	98	\N
3918	gs	f	2024-05-30 17:01:58.456083+07	8	98	\N
3919	df	f	2024-05-30 17:01:58.631162+07	8	98	\N
3920	g	f	2024-05-30 17:01:58.808423+07	8	98	\N
3921	df	f	2024-05-30 17:01:59.000399+07	8	98	\N
3922	g	f	2024-05-30 17:01:59.173591+07	8	98	\N
3923	sd	f	2024-05-30 17:01:59.362089+07	8	98	\N
3924	fg	f	2024-05-30 17:01:59.555716+07	8	98	\N
3925	d	f	2024-05-30 17:01:59.77652+07	8	98	\N
3926	g	f	2024-05-30 17:01:59.986278+07	8	98	\N
3927	df	f	2024-05-30 17:02:00.421991+07	8	98	\N
3928	f	f	2024-05-30 17:02:01.947664+07	8	98	\N
3929	df	f	2024-05-30 17:02:02.677278+07	8	98	\N
3930	ff	f	2024-05-30 17:02:03.157031+07	8	98	\N
3931	gf	f	2024-05-30 17:02:03.543563+07	8	98	\N
3932	fd	f	2024-05-30 17:02:04.041196+07	8	98	\N
3933	dfg	f	2024-05-30 17:02:04.379071+07	8	98	\N
3934	sd	f	2024-05-30 17:02:04.69721+07	8	98	\N
3935	fhf	f	2024-05-30 17:02:05.077334+07	8	98	\N
3936	hfg	f	2024-05-30 17:02:05.484382+07	8	98	\N
3937	jhg	f	2024-05-30 17:02:05.921124+07	8	98	\N
3938	df	f	2024-05-30 17:02:06.525759+07	8	98	\N
3939	df	f	2024-05-30 17:02:06.765538+07	8	98	\N
3940	gs	f	2024-05-30 17:02:06.992003+07	8	98	\N
3941	dfg	f	2024-05-30 17:02:07.295244+07	8	98	\N
3942	dgs	f	2024-05-30 17:02:07.640202+07	8	98	\N
3943	fdg	f	2024-05-30 17:02:07.969751+07	8	98	\N
3944	sdfg	f	2024-05-30 17:02:08.289866+07	8	98	\N
3945	df	f	2024-05-30 17:02:44.848739+07	8	102	\N
3946	df	f	2024-05-30 17:02:45.055269+07	8	102	\N
3947	sg	f	2024-05-30 17:02:45.241326+07	8	102	\N
3948	fd	f	2024-05-30 17:02:45.419401+07	8	102	\N
3949	g	f	2024-05-30 17:02:45.603258+07	8	102	\N
3950	df	f	2024-05-30 17:02:45.967082+07	8	102	\N
3951	h	f	2024-05-30 17:02:46.166513+07	8	102	\N
3952	f	f	2024-05-30 17:02:46.34085+07	8	102	\N
3953	dh	f	2024-05-30 17:02:46.513413+07	8	102	\N
3954	fg	f	2024-05-30 17:02:46.722644+07	8	102	\N
3955	j	f	2024-05-30 17:02:47.025363+07	8	102	\N
3956	hg	f	2024-05-30 17:02:47.18741+07	8	102	\N
3957	j	f	2024-05-30 17:02:47.349765+07	8	102	\N
3958	gh	f	2024-05-30 17:02:47.517052+07	8	102	\N
3959	f	f	2024-05-30 17:02:47.690054+07	8	102	\N
3960	fg	f	2024-05-30 17:02:48.23811+07	8	102	\N
3961	h	f	2024-05-30 17:02:48.423236+07	8	102	\N
3962	fg	f	2024-05-30 17:02:48.573651+07	8	102	\N
3963	hfg	f	2024-05-30 17:02:48.746018+07	8	102	\N
3964	hdf	f	2024-05-30 17:02:49.096966+07	8	102	\N
3965	hg	f	2024-05-30 17:02:49.357495+07	8	102	\N
3966	sdfs	f	2024-05-30 17:15:42.508834+07	5	102	\N
3967	sdfs	f	2024-05-30 17:15:46.955148+07	5	102	\N
3968	a	f	2024-05-30 17:16:04.017609+07	5	102	\N
3969	hehe	f	2024-05-30 17:30:07.40286+07	5	98	3940
3970	hihi	f	2024-05-30 17:30:26.458143+07	5	98	3908
3971	fdxg	f	2024-05-30 18:41:22.082835+07	5	102	\N
4057	s	f	2024-06-01 22:22:14.59406+07	8	98	\N
4058	df	f	2024-06-01 22:22:14.757343+07	8	98	\N
4059	a	f	2024-06-01 22:22:14.918744+07	8	98	\N
3973	https://storage.googleapis.com/chat_app_test/bb76d7e5-95f1-4c78-ae80-142961947891-files	t	2024-05-30 20:59:26.326156+07	5	98	\N
3974	d	f	2024-05-30 20:59:34.341626+07	5	98	\N
3975	reply with img	f	2024-05-30 21:24:53.68851+07	8	98	3973
4060	d	f	2024-06-01 22:22:15.085967+07	8	98	\N
4061	fa	f	2024-06-01 22:22:15.254906+07	8	98	\N
4062	sd	f	2024-06-01 22:22:15.427919+07	8	98	\N
4063	f	f	2024-06-01 22:22:15.600336+07	8	98	\N
3976	fdgfd	f	2024-05-31 17:04:30.402953+07	5	98	\N
3977	erdg	f	2024-05-31 17:04:42.05813+07	5	98	\N
3978	dfgs\\	f	2024-05-31 17:07:40.084417+07	8	98	\N
3979	df	f	2024-05-31 17:07:40.515748+07	8	98	\N
3980	g	f	2024-05-31 17:07:40.717958+07	8	98	\N
3981	d	f	2024-05-31 17:07:40.860482+07	8	98	\N
3982	g	f	2024-05-31 17:07:41.004699+07	8	98	\N
3983	sdf	f	2024-05-31 17:07:41.179328+07	8	98	\N
3984	g	f	2024-05-31 17:07:41.303743+07	8	98	\N
4064	sa	f	2024-06-01 22:22:15.757529+07	8	98	\N
4065	fds	f	2024-06-01 22:22:15.928143+07	8	98	\N
3847	mot ne	f	2024-05-30 07:58:49.219534+07	5	102	\N
3972	a	f	2024-05-30 18:41:33.35072+07	5	102	\N
3985	sdf	f	2024-05-31 20:00:55.250101+07	8	98	\N
3986	df	f	2024-05-31 20:04:00.243474+07	5	98	\N
3987	sdf	f	2024-05-31 20:47:47.209231+07	5	98	\N
3988	sad	f	2024-05-31 20:47:47.397767+07	5	98	\N
3989	f	f	2024-05-31 20:47:47.555902+07	5	98	\N
3990	sd	f	2024-05-31 20:47:47.70276+07	5	98	\N
3991	f	f	2024-05-31 20:47:47.872622+07	5	98	\N
3992	fg	f	2024-05-31 20:47:48.096654+07	5	98	\N
3993	d	f	2024-05-31 20:47:48.249124+07	5	98	\N
3994	sg	f	2024-05-31 20:47:48.423177+07	5	98	\N
3995	h	f	2024-05-31 20:47:48.610864+07	5	98	\N
3996	gf	f	2024-05-31 20:47:48.836547+07	5	98	\N
3997	h	f	2024-05-31 20:47:49.014262+07	5	98	\N
3998	dfg	f	2024-05-31 20:47:49.187407+07	5	98	\N
3999	hfg	f	2024-05-31 20:47:49.379212+07	5	98	\N
4000	h	f	2024-05-31 20:47:49.538647+07	5	98	\N
4001	fg	f	2024-05-31 20:47:49.721524+07	5	98	\N
4002	;hishdifs	f	2024-05-31 20:47:58.4483+07	8	98	\N
4003	fghfg	f	2024-05-31 20:49:20.957053+07	5	98	\N
4004	gf	f	2024-05-31 20:49:21.388844+07	5	98	\N
4005	hg	f	2024-05-31 20:49:21.607287+07	5	98	\N
4006	dfgdsg\nsdfsf	f	2024-05-31 22:18:01.807545+07	5	102	\N
4007	hjidfg	f	2024-05-31 22:19:09.970424+07	5	102	\N
4008	dfgdf\nsdsdfsdf	f	2024-05-31 22:19:15.426685+07	5	102	\N
4009	fsdf\nSDFSD\nSDF\nSD	f	2024-05-31 22:19:19.910658+07	5	102	\N
4010	fdg\nSFSDF\nSDF\ndsf	f	2024-05-31 22:19:44.736847+07	5	102	\N
4011	SDF\nDSF\nSDF\nSD	f	2024-05-31 22:20:42.971116+07	5	98	\N
4012	dsfds\nSDFSD	f	2024-05-31 22:21:03.662026+07	5	98	\N
4013	dgdfg\nSDFSD	f	2024-05-31 22:21:49.258394+07	5	102	\N
4014	dfgfdg	f	2024-05-31 22:23:08.62467+07	5	102	\N
4015	fgdfg\nSDF\nSD\nFSD	f	2024-05-31 22:23:12.545481+07	5	102	\N
4016	sdfsd\nSDF\nSDF	f	2024-05-31 22:24:18.761593+07	5	98	\N
4017	l;fdlg\nDFGDF\nG\nDF\nGD\nFGF	f	2024-05-31 22:27:51.123318+07	5	98	\N
4018	https://storage.googleapis.com/chat_app_test/815600a2-8ead-41d0-999c-ce71a0024686-files	t	2024-06-01 19:04:32.261103+07	5	102	\N
4019	a	f	2024-06-01 22:12:55.799234+07	5	103	\N
4020	fd	f	2024-06-01 22:21:55.530927+07	5	98	\N
4021	gdfs	f	2024-06-01 22:21:55.866592+07	5	98	\N
4022	g	f	2024-06-01 22:21:56.022446+07	5	98	\N
4023	fd	f	2024-06-01 22:21:56.191908+07	5	98	\N
4024	g	f	2024-06-01 22:21:56.35986+07	5	98	\N
4025	dfs	f	2024-06-01 22:21:56.536489+07	5	98	\N
4026	g	f	2024-06-01 22:21:56.710237+07	5	98	\N
4027	fd	f	2024-06-01 22:21:56.877181+07	5	98	\N
4028	gds	f	2024-06-01 22:21:57.058226+07	5	98	\N
4029	g	f	2024-06-01 22:21:57.248793+07	5	98	\N
4030	fd	f	2024-06-01 22:21:57.404914+07	5	98	\N
4031	gf	f	2024-06-01 22:21:57.591666+07	5	98	\N
4032	d	f	2024-06-01 22:21:57.763722+07	5	98	\N
4033	sg	f	2024-06-01 22:21:57.939347+07	5	98	\N
4034	df	f	2024-06-01 22:21:58.124712+07	5	98	\N
4035	gd	f	2024-06-01 22:21:58.309946+07	5	98	\N
4036	s	f	2024-06-01 22:21:58.496272+07	5	98	\N
4037	fg	f	2024-06-01 22:21:58.692545+07	5	98	\N
4038	g	f	2024-06-01 22:21:58.887879+07	5	98	\N
4039	fd	f	2024-06-01 22:21:59.07959+07	5	98	\N
4040	g	f	2024-06-01 22:21:59.271824+07	5	98	\N
4041	sdf	f	2024-06-01 22:21:59.457439+07	5	98	\N
4042	g	f	2024-06-01 22:21:59.643835+07	5	98	\N
4043	dfs	f	2024-06-01 22:21:59.825552+07	5	98	\N
4044	gfd	f	2024-06-01 22:22:00.022327+07	5	98	\N
4045	sg	f	2024-06-01 22:22:00.181385+07	5	98	\N
4046	sdfas	f	2024-06-01 22:22:12.818092+07	8	98	\N
4047	fs	f	2024-06-01 22:22:13.003937+07	8	98	\N
4048	a	f	2024-06-01 22:22:13.1515+07	8	98	\N
4049	fd	f	2024-06-01 22:22:13.312622+07	8	98	\N
4050	sa	f	2024-06-01 22:22:13.481411+07	8	98	\N
4051	fa	f	2024-06-01 22:22:13.648186+07	8	98	\N
4052	sd	f	2024-06-01 22:22:13.800808+07	8	98	\N
4053	f	f	2024-06-01 22:22:13.960208+07	8	98	\N
4054	a	f	2024-06-01 22:22:14.123365+07	8	98	\N
4055	ds	f	2024-06-01 22:22:14.281325+07	8	98	\N
4056	fa	f	2024-06-01 22:22:14.440455+07	8	98	\N
4066	f	f	2024-06-01 22:22:16.105643+07	8	98	\N
4067	ds	f	2024-06-01 22:22:16.286896+07	8	98	\N
4068	fd	f	2024-06-01 22:22:16.468819+07	8	98	\N
4069	s	f	2024-06-01 22:22:16.636577+07	8	98	\N
4070	fas	f	2024-06-01 22:22:16.807063+07	8	98	\N
4071	d	f	2024-06-01 22:22:16.97614+07	8	98	\N
4072	fs	f	2024-06-01 22:22:17.131103+07	8	98	\N
4073	mot hai	f	2025-06-01 17:07:39.381132+07	8	102	4015
4074	lolo	f	2025-06-03 15:54:29.887156+07	5	98	\N
4075	heheh	f	2025-06-03 16:41:31.10673+07	5	98	4072
4076	lala	f	2025-06-03 16:42:05.16811+07	5	98	4046
4077	lalal	f	2025-06-03 16:44:57.673133+07	5	98	4061
4078	gaga	f	2025-06-03 16:45:11.671854+07	5	98	\N
4080	dsf	f	2025-06-03 16:45:52.804594+07	5	98	\N
4079	fsafsdf	f	2025-06-03 16:45:51.318911+07	5	98	\N
4081	dfgfdg	f	2025-06-03 16:46:06.046877+07	5	98	\N
4082	hehe	f	2025-06-03 20:09:01.410518+07	5	98	\N
4083	sdfsf	f	2025-06-03 20:09:16.868234+07	5	98	\N
4084	â	f	2025-06-03 20:09:30.45937+07	5	98	\N
4085	dfd	f	2025-06-03 20:09:58.169338+07	5	98	\N
4086	dsafds	f	2025-06-03 21:47:46.338914+07	5	98	\N
4087	ss	f	2025-06-03 21:49:07.848477+07	5	98	\N
4088	dfdf	f	2025-06-03 21:52:15.709314+07	5	98	\N
4089	f	f	2025-06-03 21:52:30.340028+07	5	102	\N
4090	hhe	f	2025-06-03 21:52:53.80592+07	8	98	\N
4091	sdfds	f	2025-06-03 21:53:17.03125+07	8	98	\N
4092	;fgd	f	2025-06-03 21:53:42.426284+07	5	98	\N
4093	sdfsd	f	2025-06-03 21:54:06.017645+07	5	98	\N
4094	sdfsf	f	2025-06-03 21:54:10.030574+07	5	98	\N
4095	dsfs	f	2025-06-03 21:54:11.484778+07	5	98	\N
4096	slkflsf	f	2025-06-03 21:54:18.372913+07	8	98	\N
4097	ds	f	2025-06-03 21:54:39.896054+07	5	98	\N
4098	sd	f	2025-06-03 21:54:48.913438+07	8	98	\N
4099	sdfds	f	2025-06-03 21:54:58.406738+07	5	98	\N
4100	s	f	2025-06-03 21:56:17.569781+07	5	98	\N
4101	s	f	2025-06-03 21:56:25.741423+07	5	98	\N
4102	zc	f	2025-06-03 21:59:42.276351+07	5	98	\N
4103	sdf	f	2025-06-03 22:00:00.490034+07	5	98	\N
4104	a	f	2025-06-03 22:00:15.940268+07	5	98	\N
4105	sd	f	2025-06-03 22:01:16.825037+07	5	98	\N
4106	d	f	2025-06-03 22:01:30.51907+07	5	98	\N
4107	v	f	2025-06-03 22:01:34.199652+07	5	98	\N
4108	f	f	2025-06-03 22:01:39.523637+07	5	98	\N
4109	f	f	2025-06-03 22:01:44.591121+07	5	98	\N
4110	a	f	2025-06-03 22:04:46.736051+07	5	98	\N
4111	a	f	2025-06-03 22:38:58.85222+07	5	98	\N
4112	a	f	2025-06-03 22:45:43.091515+07	5	98	\N
4113	a	f	2025-06-03 22:45:44.700715+07	5	98	\N
4114	dfg	f	2025-06-03 23:04:36.250381+07	5	98	\N
4115	sdf	f	2025-06-03 23:09:34.714681+07	5	98	\N
4116	a	f	2025-06-04 13:55:33.098041+07	5	98	\N
4117	a	f	2025-06-04 13:56:06.242993+07	5	98	\N
4118	a	f	2025-06-04 13:56:45.587177+07	5	98	\N
4119	aa	f	2025-06-04 13:57:30.082722+07	5	98	\N
4120	a	f	2025-06-04 13:57:40.402758+07	5	98	\N
4121	s	f	2025-06-04 13:58:17.837196+07	5	98	\N
4122	s	f	2025-06-04 13:58:22.613909+07	5	98	\N
4123	s	f	2025-06-04 13:58:28.263315+07	5	98	\N
4124	f	f	2025-06-04 13:58:53.594729+07	5	98	\N
4125	f	f	2025-06-04 16:47:49.260354+07	5	98	\N
4126	sfds	f	2025-06-04 16:47:53.558441+07	5	98	\N
4127	dfgdg	f	2025-06-04 16:47:58.74112+07	5	98	\N
4128	mot	f	2025-06-04 19:17:32.096182+07	5	98	\N
4129	sdfds	f	2025-06-04 19:46:03.852298+07	5	98	\N
4130	sdfs	f	2025-06-04 19:46:05.51575+07	5	98	\N
4131	sdfds	f	2025-06-04 19:46:09.697376+07	5	98	\N
4132	sdf	f	2025-06-04 19:46:12.747058+07	5	98	\N
4133	f	f	2025-06-04 19:46:21.954857+07	5	98	\N
4134	f	f	2025-06-04 19:46:25.06723+07	5	98	\N
\.


--
-- Data for Name: Reactions; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Reactions" ("Id", "Name") FROM stdin;
2	Like
3	Hate
1	Heart
\.


--
-- Data for Name: RefreshToken; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."RefreshToken" ("Id", "Token", "JwtId", "UserId", "IsUsed", "IsRevoked", "IssuedAt", "ExpiredAt") FROM stdin;
ae6b79ae-5005-4066-885f-33bd27aedf26	V7QVWOPv8gMFlcI3ZSZTMr5l6YIbiF2emjPI8KqHh/w=	f9a8b918-01e2-426d-be92-1e4e055d2817	5	f	f	2024-05-03 19:34:08.348523+07	2024-05-04 19:34:08.348564+07
553341a8-cc0b-48aa-913f-df86832e9bce	v7ZjiPLQlrmLEbTwv6qdpFz7yLFgC8dVAeQ2t1wWd9I=	a1d54092-64f8-478f-9403-ac77bddeb5d2	5	f	f	2024-05-03 20:45:26.294317+07	2024-05-04 20:45:26.294368+07
3c1c4fd3-17ad-4972-863b-65f2a9e8d0b1	yeT2gqZ88QnT4N9WGfoDkjcApUzfqRJhjhvF+2x5gio=	720cfed4-e47e-49e4-b902-189d0a42026b	5	f	f	2024-05-03 21:05:25.554102+07	2024-05-04 21:05:25.554163+07
1e149a1a-9f80-4c2d-846f-1cac13e9db01	XgE2/9nYJmmzLN6RyoJeqRqOCwPau4Jv63I5xsWN9XM=	5528f1c7-7b1a-4f16-a718-5361bc78379f	5	f	f	2024-05-03 21:13:40.386686+07	2024-05-04 21:13:40.386727+07
13e6e449-018e-48c1-a2f7-0ee68570ac04	avMGfmn1mSVqb4WnP+hutJXuQpg01sS0tEOPeBfKDOs=	b00a0588-2842-4272-b909-486a3bb74425	8	f	f	2024-05-03 21:58:41.705835+07	2024-05-04 21:58:41.705886+07
c78c26e1-6335-4076-bf4f-a29852c0ce2d	iwVJaGYjQH+US1kzmNoBgaGGfhggTxB1l7n3aJR8aqg=	53781024-e693-4a45-b8dc-ae5cd94304f4	5	f	f	2024-05-04 03:15:19.776272+07	2024-05-05 03:15:19.776346+07
7b228165-839e-487b-ba1a-919ba242dafb	K5oyRs/52rMxlGxwjx3wR2V6HTLTwDFKgKp4t6oq1l0=	4346c2ed-1533-46f1-9d1a-8a21a42b2e1d	8	f	f	2024-05-04 03:53:31.165029+07	2024-05-05 03:53:31.165094+07
b3b2ba94-b665-432e-b945-4a098e06015c	XeekLqrjWEoFoO+NOTso9+g26iPSMkbvssTFkmq3lhA=	3586187d-1ac5-4bcc-85b6-421f26e2ea0a	5	f	f	2024-05-04 04:35:26.978116+07	2024-05-05 04:35:26.978153+07
72bdb30a-5ddb-4339-a2c6-be3400fd2384	t1z3in5TVkCDqgeTwlIPIZKp28AB8DstuhIFADduqa4=	042c99f2-9019-4a88-8dae-819163972fb8	8	f	f	2024-05-04 04:40:35.190262+07	2024-05-05 04:40:35.190326+07
4ae75e84-f3a1-4a74-8859-d7782021248d	IhGDPIteVqjjxSOqCDdXdC4EfZONXYxSaeo7/ePD7nk=	4f40f1c0-508e-4f90-912a-a66acd6fc7a7	5	f	f	2024-05-04 07:34:10.03676+07	2024-05-05 07:34:10.036798+07
e9c7575d-9a62-4201-9783-af43eed1da0b	7x0qTE+p9dcx+dcNjighMulNLyE0d8bvKzXO8jFYLjs=	d383c9ff-fd87-44bf-8869-5543584b3330	8	f	f	2024-05-04 07:34:19.701593+07	2024-05-05 07:34:19.701594+07
66e041cd-1477-48aa-92cb-4e15cf01fd86	OyaPXtiaLuSNyhGpsF6Fc10RixyZ4pZ2c+tyezMyiU0=	42e5cbcb-0e6c-4ca9-99e6-806026a9df39	5	f	f	2024-05-05 14:30:45.642214+07	2024-05-06 14:30:45.642531+07
9efc83cb-93a0-4348-aeed-b64ffd42eebf	/vIjFekeNwzQIBxHww7245vEsYLqoeGGJMC4i7JnqVk=	43ba123d-f613-4e1c-9c27-f605674e242d	5	f	f	2024-05-05 15:53:33.284249+07	2024-05-06 15:53:33.284259+07
23b36af2-b601-405e-b507-68becb873aac	GMFUoFcPBD3+xM3awrmgQXr7xy9AcWd0zXZaFMqAONY=	62d8ee6d-da59-4fb1-8878-a9b8a1a47a95	5	f	f	2024-05-05 16:07:42.887182+07	2024-05-06 16:07:42.887228+07
65f2cad0-d2c7-4216-908b-dc910d971d98	biU2Ro5K53YKzL/lpX+okOU7YrXPRZrVpLxh5xRgtlc=	cb9e0a29-ba55-4b79-93de-769adc3dc0ac	8	f	f	2024-05-05 16:30:55.595807+07	2024-05-06 16:30:55.595808+07
b846650b-235e-445c-9056-a5e2863270e6	N/2stPN79JDaykf+6x55W/Vq9cIY5mnrWZZpKUbUyWI=	8671adea-a396-419d-b4a5-1aa35b173d75	8	f	f	2024-05-05 17:35:37.135962+07	2024-05-06 17:35:37.135966+07
73ccfd90-edf1-48cc-a1c7-dd37dcb24652	j5klvD2+WDtLwliWxt99w1Q/HsOp/p5QSTH3Z3eY/Eg=	a6a3cbab-2ed3-4820-a5c3-7e1e8926f3f6	8	f	f	2024-05-06 02:09:42.178014+07	2024-05-07 02:09:42.178091+07
4e250c99-3035-467b-8b8c-593fe4a9916a	sraG7KZzR65f966ILzoFIc4wJNsida53cte41hqjY8w=	32767569-9cc9-4a3d-b101-3d458c3d5d7f	5	f	f	2024-05-06 02:09:43.597053+07	2024-05-07 02:09:43.597055+07
83cd908b-8143-44fa-8ca0-095e64a0c8c4	G3a0lMr8tTHoVfpNna6kxAUxeIFgRW23mKK3sI+fLB0=	20042027-e06f-4141-9bdc-2c48368e3b66	5	f	f	2024-05-06 03:10:11.496588+07	2024-05-07 03:10:11.496635+07
f175aa2d-bff1-4deb-9962-40158816cf8a	NvfpFK30DIE1P3MLDcAVdbL+6j/w8zJbv7nrPLiAR5E=	eb1e6c47-40f8-4212-844b-8fc494be5aee	8	f	f	2024-05-06 03:20:14.513328+07	2024-05-07 03:20:14.513374+07
e5f85489-a85d-4e40-8d43-5f507fbdc25b	Q20Lxm+EZYypZef41SLjiz5r3/Th/9s6QI3326NiaFI=	eb6fd4b2-334d-4038-b8a6-c509d6679d0e	5	f	f	2024-05-06 04:11:13.686597+07	2024-05-07 04:11:13.686652+07
adf1e516-4e1d-4b8f-9678-e006773f24cc	rvkA/OVeqNE/3OpXgAUIqxTbQX05N7tRsS3FxSg9F9g=	6bf69f57-ddc0-467e-84c0-fa8609e72844	8	f	f	2024-05-06 04:23:06.530905+07	2024-05-07 04:23:06.530907+07
43c27289-dfb1-45e4-946e-86e437de166a	c0rkSBYmD2wjnZNoQ8f0qszyR42zcvBK9Qk9T9Pppuk=	75bc9d9f-3178-4d82-8d65-34a6f16fb0ca	5	f	f	2024-05-06 05:18:48.777104+07	2024-05-07 05:18:48.777154+07
0e02dab1-5094-4f2a-a5f5-851519c83a5d	Z//ZybYMBeHZcCPrmIcunIduoVqYW+MnMZtJVGSaPJw=	ea293154-75ff-4911-8c3c-becb5794dbf8	8	f	f	2024-05-06 05:24:15.890477+07	2024-05-07 05:24:15.890479+07
77ac000e-2b3d-4b3a-98c5-eece2d80d2dd	l3ItC2273p6nFEFrf0JgHOEtLDvPr+hxpUPg3JhHmqI=	d76bc1ef-fc33-4d7c-8681-01918ef2566c	5	f	f	2024-05-06 07:05:23.363914+07	2024-05-07 07:05:23.363916+07
79bb4975-5b72-4f5b-bbd6-7eb6800e7147	tFGlBFWfMMd7TfSXvdPz2vIKVFAxTaOHAFHZuINGMgU=	198cbbd6-d81a-4607-b8bb-2efacd5f9614	8	f	f	2024-05-06 07:05:39.515498+07	2024-05-07 07:05:39.515499+07
bafca2cc-14af-40e4-82d6-7f52bf0171c6	F91LySLMWcd+MAft0W8IRiYgIVFH25v0lznlt5DDaB4=	fc225ab7-8bf6-48af-95b4-771d80aa9805	5	f	f	2024-05-06 07:15:12.832426+07	2024-05-07 07:15:12.832487+07
4a0921f7-f9d9-4e9d-a2dd-98d34d6b3ec8	UrdM8skF16CmSNJbCNKz3i/zPu1Q5fS6lsW3vkkjMfM=	534e0f25-0326-484c-9829-915031ff152e	5	f	f	2024-05-06 12:08:41.995106+07	2024-05-07 12:08:41.995182+07
98f72e3f-c7f6-4bf6-a992-f965f462c4d3	9VYAx9UB6F5P2PrbKGqd46eRE0dPZeKZvmDrid6uIl4=	643c28b3-c830-41b7-b9ab-9e8ff2330cfe	8	f	f	2024-05-06 12:10:08.262784+07	2024-05-07 12:10:08.262787+07
83108905-2e16-42d2-848d-28191240c46d	33Z05zeX6z6Ogzc/l29xVE+bowcH/8WgkNGYRoPG2Pc=	2b053a99-4a76-4468-b835-92930d9e54f9	5	f	f	2024-05-06 13:19:15.383173+07	2024-05-07 13:19:15.38321+07
09bf310a-f4a8-4bbc-854e-f19c6562ccc4	HYoF81FQF03+7m4m8hWlSYEIjojOaTtA4SryIQHgGao=	5be70cf8-5495-4ad5-943d-5dc9603c1dfd	8	f	f	2024-05-06 13:22:05.936471+07	2024-05-07 13:22:05.936473+07
e0c8294f-fe05-4400-80d6-805ebb7e599d	3OfOpGo7zS99q5G2W5bJJavrup5T8lsi2j22fLbTjYI=	53a3f4ec-325b-4468-a359-38e4cc9bd9ed	5	f	f	2024-05-06 14:23:12.032789+07	2024-05-07 14:23:12.032793+07
4d8c933f-524e-4ac8-8b02-5fe6e5bbcea6	gobbFUrSf/4VyBieCq2zwiOLreYjIpwypAEJncsBR5M=	7fefe871-05fe-48b6-9365-38a59191d79c	8	f	f	2024-05-06 14:29:43.028075+07	2024-05-07 14:29:43.028121+07
941892c7-af4c-4c4e-b23c-db70206da898	B+oFjimzxB9tfRoHerQ5vdfikCcqvMtZj6xrNMTTtoM=	e14ca116-42d8-4a05-aa1d-ebad109ed40c	5	f	f	2024-05-06 15:41:27.556133+07	2024-05-07 15:41:27.556136+07
45d2aedb-c371-4d6d-a5e9-a5aeb64b4f10	u1P/v+FH3g9G9mjVXJqEXOyUlIiYKWGKLXHwupLNLic=	979bdf77-49a9-41af-935f-ee3ba80fa33e	5	f	f	2024-05-06 17:39:01.404035+07	2024-05-07 17:39:01.404037+07
ac75c79e-3abb-4f0e-9959-c08aa38ea32a	6N75IMVXee50t/KIUbNSR8gUOS8USVVoMPwFr5kSYLU=	d0175411-af3e-4291-aad9-45b18683a9f5	5	f	f	2024-05-06 18:40:40.333408+07	2024-05-07 18:40:40.333409+07
a81189c1-85cc-495d-8cd8-8b8c6cc1d86e	f/P1nN6MF5ncQlVJoCWQoWoql1TAMS7lmGAJmr3579c=	da84971a-b440-462e-8042-96a86a9564d5	5	f	f	2024-05-06 20:21:20.800273+07	2024-05-07 20:21:20.80032+07
2acac45a-9bac-48f3-bbc8-c8ee566e4aca	PRcYoiaXdyLOWoVun3CzkhvOw1SAlVQnE09sqwSLOUw=	18c969d0-a7ae-4fd7-a0b0-0ce8eb5f3129	5	f	f	2024-05-06 20:52:53.206085+07	2024-05-07 20:52:53.206137+07
55e07a1b-086e-4478-a7c9-c25116770dab	tXK9iyTRw3mho37IVuIbCC9Q9SlM2N50Sq3eSfPpZnw=	82f0a20d-5c91-4654-b7ec-694844702708	5	f	f	2024-05-06 22:11:41.112392+07	2024-05-07 22:11:41.112441+07
109b4fa0-4484-4edf-8340-ace2380b5c83	9tkcFDliJ5KS/1raKHuDfaGL9WHlXigDBa+DZX3f6Ew=	0e651668-5db7-4e16-ac67-5545730add3f	8	f	f	2024-05-06 22:12:13.392281+07	2024-05-07 22:12:13.392282+07
ad3d80b4-86fe-4653-8244-5e30c332fb1f	h50mIVuaoFtDpIrPgoHGS3CDUf1GAQdg66vKguA/jD8=	243856f2-7f21-4576-9c21-a9d5ef844047	7	f	f	2024-05-06 22:13:16.051064+07	2024-05-07 22:13:16.051065+07
d238482f-86f9-4c1f-bf15-e1d17d658d84	wXDRldwNEgOqvvPcUUEigfHgw5/P9TruRrdkycyn41o=	e442dc00-8784-4100-83ce-4bcdb47ed27a	5	f	f	2024-05-06 23:34:53.380921+07	2024-05-07 23:34:53.380923+07
3112a45f-aa09-448f-90c5-da79a76760ba	y6iKdfC2vSdo61n+AEzylEigPrbqzdDuyChN8DD9t44=	755aa57d-254f-4da7-b647-f32ec8f6699d	5	f	f	2024-05-06 23:35:00.17023+07	2024-05-07 23:35:00.170232+07
5824d4a2-a878-4d84-89d0-ebacb980a5b4	etxfhe/ZyUG3UlDqKwcqy4BOiYxoFxTWthOZMGcfJMo=	48442382-5689-4cfc-8bac-b8f468a3a5d1	5	f	f	2024-05-07 14:42:16.63257+07	2024-05-08 14:42:16.632618+07
d840762c-2b5e-4744-ba30-b4a2bff4a06e	obUXo1D5BygEkzNvzFAKsGaflpeDylrSY+R17eEVFdY=	ff4958d2-a797-4c80-94c8-1540f65bc179	5	f	f	2024-05-07 16:08:40.3306+07	2024-05-08 16:08:40.330664+07
7c247677-ee06-42f4-bbd4-c7bea7054c92	yoUYXOhEzveVlYOPY6Wpt72Cx7NLSVI6SaN3dyd158M=	db2b7f01-bbb7-44af-8f34-c5d56d579d96	5	f	f	2024-05-07 16:17:58.897717+07	2024-05-08 16:17:58.89778+07
b2d62bf4-e70a-47cb-b8bf-1edada1b3a9f	Dh60tJjlidbDS+p7ZS517GPdrf+yQl/ziwVaiJtyB9s=	6067286c-2b33-4f48-81bb-6f598d564473	5	f	f	2024-05-07 17:33:47.436189+07	2024-05-08 17:33:47.436238+07
765b8f9b-058a-4a03-97c5-85a27aa2aee6	5AD97uZTaYmGM/G/xJ6wIHGqEYQDP294hmTWZJFv5hU=	feb387de-a62a-47e9-9449-37628ebe9b8c	5	f	f	2024-05-07 18:37:06.216247+07	2024-05-08 18:37:06.216249+07
6916770b-0830-4f1e-a30a-607141a41b37	2aLpNkboaIejaSBsea22cwx0modqUKpEjNsNMrziml8=	7ad45c45-a5a8-4f59-b9cd-c141f95e6f5d	8	f	f	2024-05-07 19:01:28.801384+07	2024-05-08 19:01:28.801387+07
8b43e423-dd1f-440c-9f45-df13d07db801	6muvmQ7dR4Y1paPvryrvYCzfLZInknEYz1nRHHdMSUY=	847f35cb-b1ef-415c-8591-a4c60f695811	5	f	f	2024-05-07 23:15:25.901145+07	2024-05-08 23:15:25.901186+07
047a42cc-fdf2-4e77-812f-eaa8f87c2b78	Qt4KUX6LN3fZm3ncjBKbBDCPBWpKkFyVjPQ3cdBN3c8=	b235c650-6849-4cc8-8e69-9d6199071158	8	f	f	2024-05-08 00:28:36.817031+07	2024-05-09 00:28:36.817093+07
b7eade76-aa30-474a-9c59-71bee52bdc98	DWxY6TCnapLM8KRju+1CsA6303skGQbpSFa5VMdhhfs=	941419e4-ad08-4ab8-aac2-46a10d802151	5	f	f	2024-05-08 00:28:43.001176+07	2024-05-09 00:28:43.001179+07
4bb195f7-2b2d-442a-a9ec-e5488ae16dca	JTycek1oWqzvZ3fF9Ztjiwlrm/IpXoz2UMtugeTM5d8=	a7b77fd9-2617-435a-8d22-b1b42d9e829c	5	f	f	2024-05-08 01:57:52.058906+07	2024-05-09 01:57:52.05895+07
861b014a-f46d-45bc-b7ff-81cdd06110df	vXxyrNNLcmNq0mF9yZ95RgIeRFagAisBTSIpAG5Anko=	d817c479-3bc2-4a1c-82ee-8a4aba686242	8	f	f	2024-05-08 01:58:07.426899+07	2024-05-09 01:58:07.426901+07
3011024d-07bb-49a6-bd1e-f2a638b9e765	QkdqnV4fecfh64f1K7Dhk+oDQHL48joLYrH02RAc5DA=	85943697-48a1-4b3e-b745-dd644fd6de6a	5	f	f	2024-05-08 04:34:47.785554+07	2024-05-09 04:34:47.785627+07
f7319703-4abf-4063-b479-a49b21931a8a	8TZq+Q6/gj7/X+aPoyVtHSRDGnPBOe/iF1tof6we408=	df9bda59-2361-4ee4-991b-ea5a5ac7ba02	5	f	f	2024-05-08 10:23:04.267298+07	2024-05-09 10:23:04.267352+07
6a2e09d1-fd98-4de8-bf8e-f9ecb76b8ec7	4jaQUi5J977ei9Ht6czfyrGGUpafrId3P2B5b5JN8Vo=	13429623-800b-4a76-a460-666ae9a681b8	5	f	f	2024-05-08 11:10:22.745446+07	2024-05-09 11:10:22.745522+07
e55aa09b-f42e-4b43-9f7d-1f47d4242318	kX5JT8O86WuT7mvpWRIxZsQz12B5HVwUdxY+l3yD5ws=	abca7a3a-c769-4a87-99a7-692b2031ee50	5	f	f	2024-05-08 14:00:32.451165+07	2024-05-09 14:00:32.451215+07
53769266-633a-4578-8c39-ed3bd09189fb	ulzcypSy4yvIAD1PT/D4r2rPdEYJq3cw8MGkrz7FlyA=	303185b7-929c-4343-a271-74e85fea8f5c	5	f	f	2024-05-08 15:02:02.475722+07	2024-05-09 15:02:02.475725+07
9306ba6b-f4cb-44e6-9e61-e49ed3b117bf	/FuJ5PoR43uu8kCknU/ZXt6UBHRilp9n+jEb8OXpNcc=	69eec126-5503-4446-9220-d092f344b955	5	f	f	2024-05-08 16:59:37.926529+07	2024-05-09 16:59:37.92661+07
571cb85b-cac5-4d85-b8d7-4d5d292bba6e	VJa9LeZUgjI+yGg6JzkZ+gwlCBPRf1lcM8IK1ouI0aA=	c10c988f-c90b-414c-84b3-2b108b401754	8	f	f	2024-05-08 17:05:41.740368+07	2024-05-09 17:05:41.74037+07
f9d9a7e5-da0b-4ae1-8591-e331110e1a34	mYyjcxz43iTCb2rkgrcnMyzlkShVAJ+x8cRTH43Gioo=	df035397-3cff-40b6-a4d8-53f069afc70a	5	f	f	2024-05-08 19:00:39.143699+07	2024-05-09 19:00:39.143701+07
f6e0a3cc-3d33-4c06-8d60-717f953c10d8	JL45tYKCDsD3qD51RfuwTtf4AKD/ZNfa/lrkvQjC4UE=	47dc1ed0-6505-4abc-884b-cc20d69cecd4	8	f	f	2024-05-08 19:00:58.623014+07	2024-05-09 19:00:58.623015+07
efbd055d-ef4b-46e4-9c56-ea21d05a56d7	q91s6n6SvEFultlDUJLouU5jam6zRBnbeKLGXbHAKdU=	7c4329c2-2089-44f2-bb28-0100aea03977	5	f	f	2024-05-08 20:12:43.434595+07	2024-05-09 20:12:43.434597+07
1a49c7f7-2c49-4f9e-ae39-1033b6deccb1	yT4CsAr4gehXSAZKR7fkaXNfIWkr+iaG9fnQdFw6/j0=	0fdeb071-779d-4f2b-96a9-8a472e1f1532	5	f	f	2024-05-08 21:14:56.517131+07	2024-05-09 21:14:56.517135+07
2708ffc5-6710-413f-bd43-a0a1dfc6916b	eq6g+/i7N0OPdfxFGs8HmN2eDlwevybG/h38C7YKLY4=	037e8579-aa96-4dd1-bc09-c9ab93172b61	8	f	f	2024-05-08 21:54:18.516926+07	2024-05-09 21:54:18.516928+07
eb01a7fc-d92a-4c43-95c8-947ffb0b58ee	YnHALz0c2VIMpTMYQwZSzIzIAQh8C0SNxdPDj/1RV34=	823e5d24-bf23-4778-be2d-e3513566c528	5	f	f	2024-05-08 22:26:26.577625+07	2024-05-09 22:26:26.577628+07
de7b825b-4ab5-43de-9a67-6f52b16fd8a1	cMuVDK4wtFhxos3SuStrG1H8ARgTRHSozagGCuqUBCQ=	79b4680d-8cd8-4159-ab44-7a5aa8a3def8	5	f	f	2024-05-09 01:32:27.418447+07	2024-05-10 01:32:27.418494+07
99454296-64b2-41c0-9d91-7068e92dee53	cR82XbKfIqOZus+BSpNGTXx7gE2Hz4whXpw+O/0NngA=	ef6b9c01-b9c9-4d07-ae88-6dae93b87e9f	8	f	f	2024-05-09 02:08:05.79448+07	2024-05-10 02:08:05.794482+07
3cd18fc7-8af9-4ce1-b7cd-df20f57c8652	K24OUHH+gQsljCvQ2rNO4WFzpbHikrirrrDrVMZNMSY=	122ca464-6ff2-4125-b0a9-58656e2d0a98	5	f	f	2024-05-09 03:05:53.77148+07	2024-05-10 03:05:53.771484+07
00ffff3e-fd69-41ad-87a8-840d9a3da14e	DRd1vHJPQLWUrGXWL/ncPaGeb5BH0P69BoLDhfUkse0=	b67938d1-7679-4c4b-83c1-4804da27508c	5	f	f	2024-05-09 03:16:27.500013+07	2024-05-10 03:16:27.500059+07
37421586-8523-4a26-95d9-4acb5f1064d5	dLbwQz7k+Jb35xSOnubrs+Ikii/UDy8GyR0Gjyq2rto=	178a17f8-7aa8-4288-84ae-0f34857d9d73	5	f	f	2024-05-09 08:55:13.71093+07	2024-05-10 08:55:13.710975+07
41a76cfd-70b9-4b17-b339-8b2bbf6b43f5	eKe/rzcvo3hxDdxAkAOOXPS4WTRxieCsCNIT4++M31M=	c1657d25-94ce-44f0-8eec-23a0abbd496c	6	f	f	2024-05-09 09:07:22.661103+07	2024-05-10 09:07:22.661171+07
de689b11-4294-4db9-bad8-f922fd23cb4e	LSRO4jaTtGKxNtF7rirDJ6WNRkH4Tf6bxpsX8en9Kck=	3b9d2f14-9cbe-470c-b2c1-1cd5c43c3ea2	5	f	f	2024-05-09 10:18:29.158389+07	2024-05-10 10:18:29.158432+07
8d53291d-be1d-46bb-ad4d-02408fd3d64b	01wmD3eRxHvdRTJh6YTFov8Y69ew+d7NV+PE0pK4kdg=	2dafebeb-46e6-497d-870a-23cd3a29f9ad	6	f	f	2024-05-09 10:18:45.207566+07	2024-05-10 10:18:45.207567+07
2035aeb4-8f30-4b4b-8520-a494f370d48a	eJmW20ef1sSuIcuEKGfpTOKX0q1B3R1kYkxPn/yxKzI=	a34054f8-71e3-41c3-a392-95b2c1438018	5	f	f	2024-05-09 11:18:31.36422+07	2024-05-10 11:18:31.364222+07
f89c8e2b-4188-40d5-90b8-25de8387108b	TbiA0ODnCG/M53q4l3xP3w/Zsx4FW0CEolsb8xwpiWw=	6f29edae-4ea2-4df7-9cb1-0bf9752223e1	8	f	f	2024-05-09 11:25:41.039029+07	2024-05-10 11:25:41.039031+07
6e3cbb22-3ed3-4e6b-a36c-779bcb1786fb	C/QdEPfrgDUfvrFvFvi+s3XetBBC0/axK4dM3rX6wxM=	5d2617e0-6ca5-420e-88a2-49f454238632	5	f	f	2024-05-09 15:10:15.994671+07	2024-05-10 15:10:15.994676+07
8a27fcc3-9475-43ca-932e-fd9dce369c9c	VihlpH8+XPQUIU1nBtNvMODNuKUKwiHnwQLXHzeP8ak=	263ea3bd-bc9b-48f7-8af9-49d8c9f38483	5	f	f	2024-05-09 18:28:15.441697+07	2024-05-10 18:28:15.441701+07
fbf1943e-50b0-4ef3-bbce-2a1e50fc1947	hj+PozvyXDgL1luRMK9x1YfTZLfAmLcRW9vuyf+vrQI=	fcff8c77-ec65-44c1-a551-4aa924fe7ad3	5	f	f	2024-05-09 19:10:27.113127+07	2024-05-10 19:10:27.113166+07
d1b15aeb-d091-410e-89e4-1e8239d2ee89	PHV+MoF3QLm1Lxkr6P3mQIC2Go1H+8dkuKJupyEM7Vs=	0d9b1a2e-78f5-4ea8-9afd-f2e7d4cc3cd6	5	f	f	2024-05-09 20:39:49.060589+07	2024-05-10 20:39:49.060592+07
505a7dfd-cd81-4318-a22c-84b6e9468abd	VrPtWLFgjmLijhSWSsUPibRFHkBsGQ/hASyDO87Y4sg=	00b3269d-89d6-47c6-a4a9-ed28ed27c4ea	5	f	f	2024-05-09 21:42:37.708088+07	2024-05-10 21:42:37.70809+07
f580412c-f622-43dd-b72a-deaff89529fe	jDOp/a+oYofYaXYhk2L5j/WR+7WTBrsr3UuTf6/fXxw=	f874b355-ad67-4510-aeb5-8bdd3d113065	5	f	f	2024-05-09 21:45:53.731363+07	2024-05-10 21:45:53.731365+07
1cdc8a7b-866c-4750-8ae0-4e5f52141c09	6PQBAm3mH3NKT2/UzD7bv8ShCq5EuiqCLmxqLZbEKmM=	41ec9297-b340-42e3-ae0f-19e360a0b5c6	5	f	f	2024-05-09 22:00:03.891243+07	2024-05-10 22:00:03.891248+07
256b28da-f7bf-4863-aa80-ae44e4cb174d	5UNSdEQ0AxCECb9YgsHS7gghA6xC33JSbErdsfPUIyk=	cc4a7f9b-c30a-4228-a468-b0c7a47e370c	5	f	f	2024-05-09 22:08:43.957952+07	2024-05-10 22:08:43.957954+07
bb4e9a67-88ee-4a6b-8b80-6b7019634f2d	uxj5CBL2/r/4EkuLR01qBwULDNmpeVetTI9PdopCKMM=	060e8c8a-b894-4f20-9ea7-f2baa9c4d121	5	f	f	2024-05-09 22:54:37.589243+07	2024-05-10 22:54:37.589246+07
b58c46b0-57ea-4ce3-8262-4d0b6fc7a269	qQIEBYTiTNq64lFWgmUOcjX2qCEU6Wvrir/8JnFxrLI=	f7bc3ef2-8758-4d47-a334-cc3629bec870	5	f	f	2024-05-09 22:54:41.486938+07	2024-05-10 22:54:41.486939+07
8fde1635-5936-4925-a70e-052aafaabd37	uk6D/mf9KHXq8iN+sPTzXK9ZS/vH2mKXhXoaLXMxNAI=	156878ee-daa5-43e6-aca9-059614ba4068	5	f	f	2024-05-09 22:56:12.649676+07	2024-05-10 22:56:12.64968+07
9a33a508-ed35-4fa1-8fe3-0e52017dfc48	h9GkcQUg06aX4XQdXDbJ+wwP2Ufb7JeCTCOOGPgI5qY=	4f8ca0e8-bcb8-4aad-9efa-7f58033f15a4	5	f	f	2024-05-09 22:58:46.939253+07	2024-05-10 22:58:46.939254+07
95df3597-c042-4195-94ef-6146afa53a64	O4wmfc024Ec1bSPb3GLLjgK0txHzQvZUKdjU8506ySo=	f00bde98-fee2-4a1c-a811-f43d2ba82a83	5	f	f	2024-05-09 23:14:16.162318+07	2024-05-10 23:14:16.16232+07
4fe69be0-c958-4fef-8340-05754fdc7b1a	HZJVg36u4ua+qtMyYj0XlYa8CVsaJKAwAGB5osJOxhc=	c802123e-c5c7-4f32-9850-bb4e072c9e5c	5	f	f	2024-05-09 23:21:02.111323+07	2024-05-10 23:21:02.111326+07
63142cd8-24c4-48f2-8471-277e066e6a33	BxRujJaZ9jAdyo53q5AOGB/VcynKAPb+KWDZ/pYVNZw=	199d6c16-83a5-4a25-ad06-babac0cf7c11	5	f	f	2024-05-09 23:21:05.381373+07	2024-05-10 23:21:05.381375+07
efeb9ec5-efb7-49ca-87f7-9048a14c289c	mSLz/lRq5YPWwf64vmVIJT5K5mREFmblSnz6/sXs85M=	946367a8-497a-47f6-bbee-7838432cf8be	5	f	f	2024-05-09 23:22:47.550641+07	2024-05-10 23:22:47.550643+07
8572d226-579f-4704-8b1c-26e12b03b0bc	AFVM6/7zgEGJYUgGKF0s9SVsaBJplw1OIt/+cNTtsB8=	4078e76a-f0c9-4829-9507-1a5cd18e259b	5	f	f	2024-05-10 14:25:47.806721+07	2024-05-11 14:25:47.806766+07
a5078787-e68e-41c3-974c-0bd357aaaabb	9dTTeWc/Y8NaJ7DLnzO86+WTUvzah5P0ioh1RzmA5MU=	44e1e3db-02b9-4689-aa74-a63f438f2444	5	f	f	2024-05-10 15:01:29.946309+07	2024-05-11 15:01:29.946311+07
5704bcdf-4670-41c0-bdf2-f8787e1cf694	rRLPPToVynlZYsXaTkuneZBAygVd7xqPyZ+mTRf6btQ=	71d3d22b-4163-4b18-b7c2-91356b1fbcb7	5	f	f	2024-05-10 15:05:08.841626+07	2024-05-11 15:05:08.841627+07
612e54e9-5ca9-4d90-b10b-ecfef234005c	tw5y/6x81zphoi/FtkIEEl/TRIvm6VIB8kIHldC/O7c=	a60263f2-bb42-48fa-a2f5-3ebaf5a8b798	5	f	f	2024-05-10 15:10:31.7745+07	2024-05-11 15:10:31.774502+07
23624892-1573-4d93-a3b3-22a8c68aa306	abUbX4x2efFYanSUdhzlo3ghDqBpoE1g1YCZIOZnA4A=	48ab8ce5-d338-437c-a45a-171f114b7898	5	f	f	2024-05-20 20:16:20.712347+07	2024-05-21 20:16:20.71235+07
e9dfa0b3-4d85-4ab1-88a9-2bfafcc32686	rrIkgmc+we313yPcwDMFh8jMo1cOgQRNUErQHryb+Lw=	b8947bdc-6926-484f-b844-cebbcabf4364	5	f	f	2024-05-10 15:42:56.102601+07	2024-05-11 15:42:56.102603+07
f8ca62ae-fe5f-4989-b0a5-16e295789b73	FfVlWe60qsYVogMWBjt12XArCyf0BgzeA/wOkEA/5mI=	0b0af60e-9aed-4b62-801c-ecedf561089e	5	f	f	2024-05-10 15:53:37.900716+07	2024-05-11 15:53:37.900719+07
939fdcab-b015-4554-b862-c3403bfc8ede	FKwJ3IT7frvIitf2IDClqU7kyTRBxFto6WLWW90DPE0=	b7816c97-bde7-486b-9e0d-9b47dd04a44e	5	f	f	2024-05-10 15:57:36.033736+07	2024-05-11 15:57:36.033738+07
3a9ec9e2-8ab2-41e2-bbf2-cbf1ac062358	CBahXL/yfsx94FNat5t8wya6r/JUTqXSazug36lciHY=	0e959054-dff2-413d-967e-50e01ff23986	5	f	f	2024-05-10 16:12:09.221894+07	2024-05-11 16:12:09.221895+07
33d73f53-5755-4ef7-b964-ae03f359deea	duaFY1K23uE4+wEkSS9hXqVSxiH1z7BnX9uKTSPIKE8=	45bdac75-49ae-460a-8099-bfbe75f06040	5	f	f	2024-05-10 16:13:06.634848+07	2024-05-11 16:13:06.634849+07
32a10e6f-5540-48ee-92f6-190637e68f84	29/+UKRHcchs9JoXDw9tdufn79FGPV53xrVgOwiAW3I=	da35417f-1666-47bc-b6f4-c61aa12ff208	5	f	f	2024-05-10 16:17:57.466266+07	2024-05-11 16:17:57.466267+07
6be0b352-326a-498d-98cc-cbfadfc0290f	g9awSMUCSQl73tSWE7oz6x3p7l1eKuRjTACO9AEaf18=	4c536569-51be-4612-99bd-2cc281225480	5	f	f	2024-05-10 16:32:07.063439+07	2024-05-11 16:32:07.06344+07
f99e73ce-b4b7-4731-8b1d-8428b4a7eb7d	dqr4b8e/f95dKn1hmxv583piOirqrx7HV2rMHyKh4pI=	cb9fdaa2-4d8b-4278-86d0-49d2fa440953	8	f	f	2024-05-10 17:25:10.676656+07	2024-05-11 17:25:10.676657+07
2a1e1a22-c177-4c3e-8ee1-eb8f404e06d0	tUDDZZIPILvIHYC0HSoCMXlF6eKPxM5n3VE7iVK+pdg=	980307f8-a334-42b9-9252-ccb559f9ebe5	5	f	f	2024-05-10 17:50:45.994403+07	2024-05-11 17:50:45.994403+07
a28f6cee-142c-4a82-8287-9a5026727634	MtL/yLdCCzBJw4HOkdwTeYOTdgeROMULneCHa39R0/k=	6e0ff6a6-cef5-47f7-aa5c-55bca53a0b3d	5	f	f	2024-05-10 17:55:15.855767+07	2024-05-11 17:55:15.855768+07
3d53366a-9621-43da-9a92-ec0d47feb9cd	ccN+o15ueZexVljyzcZkgVgkF46fc6TWnBTVFLYo+6A=	74dd261f-a89b-4426-a34c-1a803965a3cf	5	f	f	2024-05-10 21:32:55.675288+07	2024-05-11 21:32:55.67529+07
a45f1d58-e05d-45b6-81f5-0ffc1f76375c	vtZH4JT2vc5mZIlAcMMH/wJATbjOn+qcuKmYbdHxjsc=	112b523e-bf6d-47b6-95c8-d5a59d44da0b	5	f	f	2024-05-10 21:33:15.437305+07	2024-05-11 21:33:15.437306+07
fee34cab-511f-4d18-83d8-dc8b0bb40324	LnlV+UrxCa+P6xgFt9ppzbdg40eI0sdyRMlDZhJkpBE=	70238c99-98fe-4790-8f11-f2cb4e608094	5	f	f	2024-05-10 21:33:38.823514+07	2024-05-11 21:33:38.823514+07
13924347-8375-4bec-aa6d-409d6f02c6e1	Zx4xbRJxkyVyct8p4t6wqFXn6zUhxvB1tBOcg3RjsWE=	fe83a3b1-f469-46e3-8d44-f4f672eef784	5	f	f	2024-05-10 21:38:56.006515+07	2024-05-11 21:38:56.006516+07
fe7939c2-679c-47fb-afaa-389daf484bd4	3vjZh823VKKDzRnfRK6958dFvAE9HaxJK+IEo5fQVdk=	245705b9-11e8-4644-aae5-4e4931bc4494	5	f	f	2024-05-10 21:43:20.646888+07	2024-05-11 21:43:20.646995+07
2a1437ea-7abb-4f18-8c2e-16abf02bb3c5	js1L/g1FDa6rtFJ2EvW0cIXlmVWtnD3G3BuUCOh9LY0=	37b398c6-ddac-4cf8-a307-0b606f728f9b	5	f	f	2024-05-10 21:46:47.045904+07	2024-05-11 21:46:47.045904+07
64c34e46-dc6d-4dd1-bb04-72becdd1f898	q99gPkBZ78CIFGnN/h1iTN68Q3Z6R8+a5wZlJBG0Dwc=	70f0506f-73e8-450e-b2f4-1613d28fe800	5	f	f	2024-05-10 22:07:13.385397+07	2024-05-11 22:07:13.385397+07
94e4b84f-9f6d-4122-a5b7-5e6fdda1323e	S0HXOv5L3+K7kRUmrA+BrdU8vutCG9tMC2vS+qePB9Q=	91a4265f-6f4c-405d-a5df-4cd97510efc8	5	f	f	2024-05-10 22:10:57.71596+07	2024-05-11 22:10:57.715961+07
734e9f30-d569-4b41-aa9e-823dd41859f8	tslvi4WUOqgZkvhl6/dp1BHwn6ql8vTXTOG/qrH5BLk=	a30bcbac-877a-491e-b2a9-93a24ddc1687	5	f	f	2024-05-10 22:21:00.43067+07	2024-05-11 22:21:00.43067+07
7772d845-2532-4892-a259-9d003ea7ed39	6k99P2nXMDOvsY0Vfa82xDIA3nvJb+j0ZuSDwmJ3BfU=	d5ea09b0-bda7-4bfa-93e8-7c3efce1e2d4	5	f	f	2024-05-10 22:26:32.862052+07	2024-05-11 22:26:32.862054+07
8ae3cd46-b8ef-4b6b-ac8f-a212d456d3c1	pVxmf1/6KgrH4Mw1GvVny1JcixLwdSiBdsQcl8eg21o=	077b17e1-d85f-4e4b-beac-bf7991cbf3d4	5	f	f	2024-05-10 22:44:04.914094+07	2024-05-11 22:44:04.914096+07
8262648f-b7d3-432a-9a83-9067126ce2ce	z8gTeiEnaujUB5OLJ8a33JRQeiMs4CjEBi8ac7cbRJo=	59474cf0-f698-4ecc-b00d-bba8f28f5e29	5	f	f	2024-05-11 01:12:42.897216+07	2024-05-12 01:12:42.897218+07
786aa7e8-45b4-4583-89ef-fa67bd909ddb	97Do/CKWUegXSR93B6BKcN23VCIAxQ/2W6u5tedxt0o=	2a5b4561-c602-48c3-97e5-fd80410ff19f	5	f	f	2024-05-11 01:15:01.703834+07	2024-05-12 01:15:01.703836+07
cb6a375d-91a0-494e-bfb6-85ce654a8345	l5WOyB2P4GE82UjHIIYXY82DAcga8lCHSIOe4iRkU/4=	e5e9a6ed-e215-4d85-bfb2-79bdf8d02328	8	f	f	2024-05-11 01:21:02.527989+07	2024-05-12 01:21:02.52799+07
c33e168f-dd4d-4b47-a733-5a652e1fe678	w3g0pXtiiwHrp7ATuJ25B+zCYRAVn/YEXRzNWOgH8KQ=	590f043c-02be-4f50-bb1c-299fe37bcb51	8	f	f	2024-05-11 01:24:01.425015+07	2024-05-12 01:24:01.425017+07
8ca06b46-7628-4633-a941-2e5c510bf878	32grpQ4guRa5qICkOh4NYbh/MHiKFNrvbMeWjIaanjE=	b08cefc6-56b0-4549-bb06-b229d2e15adf	5	f	f	2024-05-11 01:27:58.607572+07	2024-05-12 01:27:58.607617+07
738cbb58-e872-443f-a255-2de302895485	/igOaXgVjEkmQ0JYmGwaBQQWpiTLHxsbJw+3wkok5Z8=	a748ee2a-7e05-46a8-a8f6-bc4627341230	8	f	f	2024-05-11 01:28:17.542372+07	2024-05-12 01:28:17.542374+07
660cdd21-636a-4226-b3df-080294a80be9	wfS9/cJuoyMR6mO8ZaXfD8VOrfkB4fzM/2qXJ9AkEnk=	4b32ac24-3195-4220-8d74-a3ccfbd091e4	8	f	f	2024-05-11 01:31:33.75746+07	2024-05-12 01:31:33.757462+07
4fa3c26d-b0ca-411f-a78a-e8becb85d3db	2GvpOs+xFCRPFSUKL0juBy9U9PKSEWKcPpys8aTvUcE=	cda30c88-37a6-4a5b-abc7-867895e44a5a	8	f	f	2024-05-11 01:33:44.62622+07	2024-05-12 01:33:44.626261+07
d8ccb6bd-5017-491a-84e2-12bc79491422	fYYu146vmYiqC2j474xsqTqynxO7ansKi+pauemQAfY=	33d2a907-eeeb-441c-975a-58d783953d0d	5	f	f	2024-05-11 09:26:53.865707+07	2024-05-12 09:26:53.865762+07
2708f2b6-e5c2-4077-b1ed-2af2d23eb592	NXFGf1hTq5PLBZ7na+Nqi8AVgAFq9gSVhicxv+6w4Qk=	2f1d6737-bc90-4416-bf21-be58329c71f9	8	f	f	2024-05-20 22:34:10.089049+07	2024-05-21 22:34:10.089126+07
e979f957-3d57-4bab-bfd6-41479995c97f	d/EgdYWA+cFb21iR/PD/mAaAzihXldhIdokbEbguG9k=	61911eb1-aaeb-4bda-9726-8749c43190d1	5	f	f	2024-05-12 19:01:08.032247+07	2024-05-13 19:01:08.032299+07
51203d25-7ad9-4ba5-81fc-739cc9e180b7	Tcda/poXsczZzB1p+NPOSHd1eHv2AcbLJdYf1kEXyVw=	2df35e58-8154-482e-916d-6a49a675f45f	10	f	f	2024-05-12 19:01:43.785484+07	2024-05-13 19:01:43.785486+07
84700cbe-859c-49b9-9b6c-204fe16d3181	p2LrbilcRJeELqxrdcJw6CpiVgcx32xA3MbhHFPrY10=	d90520d8-6aa0-449c-b29a-cc1d582ce214	5	f	f	2024-05-12 21:17:13.456678+07	2024-05-13 21:17:13.456749+07
a920ad87-93c3-4448-bac7-e2be1d263b9b	xcqOg0OEBEG9XCfgijiB2GqCK6cr4DdfKV/ONr5pzyI=	3537db06-aa95-4f6a-b8b0-419c6633e67b	5	f	f	2024-05-24 20:30:39.313217+07	2024-05-25 20:30:39.313268+07
267b1398-8171-42b5-8bb1-fbe7c1535716	ys21xaStPnL4sRfepvtZ3TDR4rkBJ3vG3WJwsm38lA8=	b427d103-7df7-4891-a730-ec88ab526108	5	f	f	2024-05-25 02:57:05.877887+07	2024-05-26 02:57:05.87794+07
181f7034-96d9-4fcd-a5c4-b31e6674181b	/v81jQ7uVivwwvEwyJCfsV8hqWyCfekYuo+usK8/nRQ=	2612943b-807c-4d19-a4d1-dfefced1dd17	5	f	f	2024-05-25 02:58:16.724085+07	2024-05-26 02:58:16.724087+07
11df388c-5e9c-4d47-9642-f2ec9706a48b	FBWJ3HSeRzWcK/b1LDCV+aPAyu7KNPDxoMBKvl7HrSk=	3092c8d1-4c53-4e96-ac58-11230768cedc	5	f	f	2024-05-25 02:58:33.189555+07	2024-05-26 02:58:33.189558+07
4e68e231-d30a-4c35-9de7-1ed0761f6ff9	JTwIJhDa3JY2Nb3IWQBKRS7ZyZ6TM8snxwfK/cNVlBc=	e7a67ab7-67bc-4efb-9d00-57d3c6c8fc69	5	f	f	2024-05-25 02:59:58.287473+07	2024-05-26 02:59:58.287477+07
e10edd6c-3fdd-40ad-bd3f-b122355dbd8a	yBXE1BCuh8atKeeLA8+VTssfcvgc7Vg4qtTXw/gEDzA=	02e5d04e-2954-471f-bfb9-b529987571fa	5	f	f	2024-05-25 03:00:31.160636+07	2024-05-26 03:00:31.160638+07
32a20a76-3e36-4961-887c-1e8135e4552d	EttvSQIIpWkMg8DbDVEyxcfABIttfyg1ptE5eMkCUr4=	50457cc9-087a-4b29-a4b5-dca829d8842c	5	f	f	2024-05-25 03:01:27.183348+07	2024-05-26 03:01:27.183351+07
889d54da-f2a2-40d6-a196-ead70ea66a2f	BeOYXEy/qvDhcQzj7d4C6Gs4ZUklh3RpjZMhH5z4Fig=	26cc866f-487d-4585-bb4e-79e68f2d774c	5	f	f	2024-05-25 03:02:55.793636+07	2024-05-26 03:02:55.793638+07
d9609124-1d09-42a3-840c-6494c3fcb2d1	7CGSS3BhgXOnaJZFbd+FzoZLszxcEaYlBV4PzmjXuRI=	df6ae5f0-d7ea-477f-b642-7a9c7f4e8dda	5	f	f	2024-05-25 03:03:42.739862+07	2024-05-26 03:03:42.739865+07
b3c09dbb-f205-4e58-916b-7b920318f29e	9+/S6GeQ1THTAm7hka44f8wL5YSaurQr62p+SZJKsWY=	393c8ee7-f34f-454a-893a-62dc836598f3	5	f	f	2024-05-25 03:04:27.865871+07	2024-05-26 03:04:27.865875+07
3d448e6b-d98f-4ad7-99c8-71731e2b8372	b8Px7IkG0VReJpyVLcUhTQXcZxXn/2pnADytYInHmHI=	c20eb57c-6616-42f0-9392-9a83fdb0622b	5	f	f	2024-05-25 03:05:51.888699+07	2024-05-26 03:05:51.888742+07
331c256b-9fc7-4afc-8abd-c6001b7b6eb9	x8gAjr3GgDWUPE+KZgvikw3CTDQS+Ae0M+rSt8LGU4I=	ba77c843-fbd5-48f7-99a2-fc493437d370	5	f	f	2024-05-25 03:05:59.246489+07	2024-05-26 03:05:59.246491+07
43dc8ee4-e2af-4e97-8071-87631e38e91e	DIwKvnros3blIH1Ng9Qzt/FIdQvtSWRKS31hWqNmd8I=	b65d6a56-af46-47d7-9c7d-b6a0b99627c9	5	f	f	2024-05-25 06:58:11.649316+07	2024-05-26 06:58:11.649366+07
800530d6-2121-46c9-9cc1-e5bbc7c4cee5	QRrhL8rgKbHMD6rCkpmkxLeerxNrexhktk2VEr+JIG0=	24fe5a97-699f-48b0-8779-cd7387a215c1	5	f	f	2024-05-25 06:58:32.756091+07	2024-05-26 06:58:32.756093+07
57fb41c4-265f-4eb6-93a3-b6a7fc0472a3	hltBoLs++Zfoq21MOczQPAxwXWeoC/XKLfTeb0dYSmU=	d557bea1-acec-47e8-a41c-36446fda6574	5	f	f	2024-05-25 06:59:11.21277+07	2024-05-26 06:59:11.212772+07
90101e7c-4745-434a-baff-168b4adeb4e5	lfHW8KTbYlgAjFmVDe46udxc1WLHqZj7CFtRE8W4IfE=	8984c6ad-3c06-400c-b8de-cbc125bbc27c	8	f	f	2024-05-25 06:59:19.367683+07	2024-05-26 06:59:19.367685+07
0b904c98-b78d-4032-af77-919d60ed2ef3	dQAXXrbX6v8UJSCw6wkCZS8LiWbsyWII5VB0nqX8h+o=	b5ae24b1-63f6-4c10-ae8c-1e5bd83dc16f	5	f	f	2024-05-25 07:00:13.631332+07	2024-05-26 07:00:13.631335+07
753655ee-5767-46a4-bbb8-364e2c38e601	nAkDKsaBckja6kF5NAeSGgpyu2+LqKyZ3+i8MlkcUpM=	2374fe62-0d48-4dce-8f71-c2787c486e6d	5	f	f	2024-05-25 07:01:47.78419+07	2024-05-26 07:01:47.784192+07
83489f01-9f50-40b9-b1ae-85718d7d61a9	YdBJqP9y8tmnYkyNJhv+/q0snPFzvA+3n77v85r13b0=	628f3d9b-ccc1-471d-afb0-5cd260b5c4e2	8	f	f	2024-05-18 21:31:06.341914+07	2024-05-19 21:31:06.34194+07
4230e608-2e89-4059-8050-4f442cee79fe	xv1q+76O5LegZH6bod9RJJBpGgUTYzI7+wewKRDvdwc=	ef33f5be-e0a0-478c-bb3d-ed73c8fb5d31	5	f	f	2024-05-19 15:43:06.544894+07	2024-05-20 15:43:06.544896+07
17abca3e-9e65-47be-9278-1d3ede4b1242	MzXPEi27dNxYaYiGRdOGme5QZubJB7zSTnCQE9y8lek=	8a207274-84e7-4864-ba20-f24a58c98a3a	5	f	f	2024-05-25 07:02:25.300955+07	2024-05-26 07:02:25.300958+07
1761a371-87ff-4d19-81eb-eed00057b4c4	4CnvtwiZpy0n2SK1NMJxh+aN7aSOm03iPA4nf45pU/4=	64e09200-d4a0-4a62-a878-62b4915c0af0	5	f	f	2024-05-25 07:04:18.214526+07	2024-05-26 07:04:18.214528+07
7ef20e0a-ef28-4e1a-adca-3293e522de42	KGVFhANag3yW+QxH3ahvbs+EbC4oTpqZXTKnI68dsF4=	1cd19e36-c781-41f5-ab55-a743ef464cf4	5	f	f	2024-05-25 07:04:39.055063+07	2024-05-26 07:04:39.055067+07
a61b58dd-dfbf-404d-9520-b3689ed7b930	YkUtEn/7rDwpXFjf3DFntfRmhzHkP779516rz51l47M=	75058421-3f9e-4239-8214-66e33612413b	5	f	f	2024-05-25 07:07:00.473036+07	2024-05-26 07:07:00.473039+07
17285bff-419f-4fa1-86d2-7ea5411fb670	sCD2mARI6Oi81v+tnhlsTizd3grqpDMIIy8W5sMwU4k=	7871f269-bc64-40a3-bf04-9c9fbd74b960	5	f	f	2024-05-25 07:09:24.188017+07	2024-05-26 07:09:24.188021+07
ac66f3b4-5c96-4878-ab4a-917336d45188	26k4VgN1PYyCNyyzclkyk5jbWVSeiilB7ndp0ydP+6c=	3932eff3-813b-43ae-a4a6-4a1b66ab0e61	5	f	f	2024-05-25 07:13:18.876801+07	2024-05-26 07:13:18.876803+07
0fac931a-eb2f-4e55-956a-76daea98876d	gC+v9RE5nxiBsBDkimwaHrnkwF54LawLgQlpHG6Qmcg=	0d54c798-a67b-43ef-a9ce-31a97d7745b9	5	f	f	2024-05-25 07:17:48.397529+07	2024-05-26 07:17:48.397585+07
b8d07e58-1752-4266-9fb9-7458edb74f03	RpFbC27T4VYem9tHHq6eXrAqg7KBJnCuI91YQIWg2uI=	33575a14-1812-40fd-851b-512c9ee51b1c	5	f	f	2024-05-25 07:18:00.211431+07	2024-05-26 07:18:00.211433+07
6d75a33d-edd4-4f9e-9798-ed5412c8e0b0	/UB/MbKFvdT4fAXNKjUSyUedwS8P57P5qH9l01myImU=	b82de0f8-7c72-44ca-a4bf-fef685bd4014	5	f	f	2024-05-25 07:21:41.349468+07	2024-05-26 07:21:41.349506+07
b43c1a82-5987-475f-9577-a1fb5e66d510	SAjs+ME0MrGaWOGohuLSMo0tqIpzmsGPeCgi1hvnoQw=	19f78b17-9746-42c2-9a79-d0616127bd76	5	f	f	2024-05-25 09:13:27.799564+07	2024-05-26 09:13:27.799601+07
91056047-9755-4f56-b5be-c83e27e4cbe1	7wN0Z8LOzrrIuoxY9PcvYWlO9Yb/TXVeWKj4ZWtDp6w=	99415be5-cdc5-490a-80c1-2c7fb99eb0a8	5	f	f	2024-05-25 09:13:36.776172+07	2024-05-26 09:13:36.776173+07
7189cf8b-52a7-4cbb-8e75-93b0690027d2	kDb9F6QOVBiTL/ps6hOJy0FYkF9pDImi2ohEYzVCNuE=	04ba9d3d-42cd-4d9c-b678-a51485892925	5	f	f	2024-05-25 09:14:29.264811+07	2024-05-26 09:14:29.264812+07
8893986f-3778-481a-b041-49b84116bd63	yGBxmk/+UiP4X1aZi9Rrt/p0GFNbsHRnTcGaNs/4fsM=	a2068c55-ac83-458f-9893-379f86f287aa	5	f	f	2024-05-25 09:14:53.087239+07	2024-05-26 09:14:53.087242+07
2fb039fd-1741-472d-8345-ff539484047c	qpdl0uODCvi3aBPWJYwAhVySP0k9sc5LbvygeYbsTbQ=	0e2eae56-7abe-47bf-80ed-3604f8dc22fd	5	f	f	2024-05-25 09:24:06.252218+07	2024-05-26 09:24:06.252221+07
6fcd6630-932d-48a7-8b95-e49238b21b46	8Jbc59zH3kZgsWLASfkIuLQBa5Azd7iPc52jn/Gz5v4=	858d0f65-834b-4997-a4d5-8722ceae0352	5	f	f	2024-05-25 09:25:14.71962+07	2024-05-26 09:25:14.719623+07
40c1b0a8-aa48-4bb7-9a65-87c82d869161	C3Dc9Ov74HsM4GwCznQ+PtOLNeeJYQUCfCULW1WkM6g=	f9e10d77-f13e-4830-acf2-f92915562670	5	f	f	2024-05-25 09:27:41.459411+07	2024-05-26 09:27:41.459414+07
37b60496-8f28-476c-8ccf-b8cb3c947db6	lYL7BTsBb0szqWm1Yc7Lkh9mChA/U5iGqXYX9nBmNpM=	1237054d-e91a-49c8-b42d-0af60596b79f	5	f	f	2024-05-25 09:27:56.157047+07	2024-05-26 09:27:56.15705+07
79be0d43-5093-413c-8bb2-815d1e449307	+UNzvF03S4hGl57xyjfr8ZRGwwoiaUvbFw0JgbhMfAM=	e9782563-b944-4ed3-a7b1-8d36faaea400	5	f	f	2024-05-25 09:32:00.643734+07	2024-05-26 09:32:00.643736+07
46921ee9-f9a5-453c-9117-170dd96a2b9a	R6sHepM/S1kPzL2Kaf0zbXp7j+w1PGBncuMSNB74+DQ=	578b98f8-c652-408c-99a8-2e536e41bd9c	5	f	f	2024-05-25 09:32:52.144049+07	2024-05-26 09:32:52.144051+07
e28e9b0c-7f40-498b-857d-87fd8366e112	8nvBrD/LtJBIXIV+Ux6ERhaBNxIbspUSt/yuO/qwY9E=	6363265e-b20d-48d6-84ce-1fc98864380a	5	f	f	2024-05-25 09:33:09.065007+07	2024-05-26 09:33:09.06501+07
4b9b04ea-b691-4a67-95eb-20a5c02f3a99	5YOVRQf9YKdrvEBu4C7e+dLVyOJoJR1IcfHj5BBb4MM=	7ad088a9-ff91-49ff-80c9-83be703aef40	5	f	f	2024-05-25 09:34:19.082261+07	2024-05-26 09:34:19.082263+07
9b6ab5de-ec00-4abf-a25f-a3d6e9a8ee40	wSPQki0PMT/8tTPhgPshFCXMiZICXwNmjhHr5UPkDgI=	972b6d29-42d4-4f48-b3da-e666bc98e6fb	5	f	f	2024-05-25 09:47:58.090093+07	2024-05-26 09:47:58.090243+07
9a89b168-1d6f-4abc-8f32-4fbca08ee3ab	9Fym9J7kjURuKA45GcULsqOSmy/YD3d1x04qjzHhUtI=	a0f13d3f-fc81-47d9-9cc6-284fc2f2991a	5	f	f	2024-05-25 09:49:04.856574+07	2024-05-26 09:49:04.856576+07
5518316a-bf9b-4193-97e5-ba8b77416a62	qFpILiThuAoTgx6jgzFzmNYFIPqG6PO6mywVxh2nch4=	1ec6f78b-efd6-4546-a3c7-b7ce3d6d8090	5	f	f	2024-05-25 09:49:35.818076+07	2024-05-26 09:49:35.818218+07
2313a595-01c4-4e97-9214-823c46f161ab	0clAHfnsiea1yyL478zD54BUwFZmHrprYHg9cIPTUoc=	f98924cf-5714-4912-bb92-ece73d988679	5	f	f	2024-05-25 09:50:09.100314+07	2024-05-26 09:50:09.100315+07
1b52e0b6-588d-46e4-af27-2530a02f058d	tkq9L+89pKO1qqeLOv74GcbnRcJcWAwllrnGIK99ukQ=	81950db9-639b-430a-9069-a3f0bb83f251	5	f	f	2024-05-25 09:50:44.610898+07	2024-05-26 09:50:44.610898+07
23228339-ae6f-48ad-894f-52307bef055d	B0c3/NZ/gVv888u1daN/NolfhP/t+OJ2SGEQytOQkms=	3929ad19-9c75-462c-9087-4dc04e5ad698	5	f	f	2024-05-25 09:51:27.773858+07	2024-05-26 09:51:27.773859+07
b4ce0677-ab8e-4392-9fa0-f47ef8eb700c	gIxqpqFx4mY4bznTNkbJSS03snN3O6HsLOT5KkEyl4s=	8e206017-b5b9-4071-a7e3-27c9fef406a9	5	f	f	2024-05-25 09:52:00.800046+07	2024-05-26 09:52:00.800046+07
0bc8364e-c3ab-4823-b79e-956b6c207b67	h9uwBauyittdt0ePoGnjIUSU7+aEH5ig/lOracWOHfM=	c228fb6f-2c8e-4717-a174-da9d78f4d20b	5	f	f	2024-05-25 09:53:12.569822+07	2024-05-26 09:53:12.569823+07
f1c27d78-77e0-43b9-80ea-0df998a4c26a	VnRPqEMi63+yXb9Od+MYdKiUVIiVjNilVpbSLbvjq1A=	02f7b0da-a1e2-45ce-93b2-c81551b032e7	5	f	f	2024-05-25 09:53:18.604934+07	2024-05-26 09:53:18.604935+07
f9704b65-e93e-4606-a775-464e57a8557d	uqvFmg2qyvFlMM9LDrNsqa87KWNWvhIwG303/9fy43k=	9a174967-5189-455b-8afb-7a414eaf8351	5	f	f	2024-05-25 09:54:54.080888+07	2024-05-26 09:54:54.080889+07
8c75083b-f56b-479c-9dba-61a8c7d5aef5	Z3CWw8beSMQKjeLbszMYGKWl96fLx5/XYtdjzrC3Mc0=	873819cb-4205-4979-9286-ac6cba60e093	5	f	f	2024-05-25 09:55:35.530106+07	2024-05-26 09:55:35.530107+07
0723ca08-2810-463e-a354-c54059a53a81	fbdQBoFYyaL8l0DITC3X3C53zeX4mtKjbbTHUOA2pkE=	6102c081-bbbf-4e7e-a44c-9e31f3f88cc7	5	f	f	2024-05-25 09:59:03.157069+07	2024-05-26 09:59:03.157069+07
f54ceb31-53a0-42f7-85e4-71d098c52d2f	wZHO4l6CO/3TQc4Ufgd5yCxp1+aJrZ259AyaUDohzgk=	7dc8d563-f197-4b22-862f-009daf0481ba	5	f	f	2024-05-25 09:59:15.429465+07	2024-05-26 09:59:15.429466+07
29e3abec-68aa-442a-8b65-10aea3309ca3	txS/NtGoyUMu+T7mfKgIUyWaPZmIlz7ael5MYHmDvRc=	200f2aeb-2739-4b54-8841-deb24acc9f67	5	f	f	2024-05-25 10:03:54.440797+07	2024-05-26 10:03:54.440797+07
0cf75705-274e-4c5d-aa74-b94532167f02	Z8eP4ITpeMviNnK9nlED2PATiQPkYYu/THbHxw5dnE4=	1b9081aa-b0c5-4e66-ad67-2e6b942a8d19	5	f	f	2024-05-25 10:06:51.217121+07	2024-05-26 10:06:51.217122+07
d6c4aadd-0cd4-4c3b-9f2d-41539be45850	9jrExeYaqMr8u6g7EtyNf25nU2hNrSEdILnqHVcFsNk=	50f2f654-79ee-4a42-8608-654c6f318376	5	f	f	2024-05-25 10:07:01.02632+07	2024-05-26 10:07:01.02632+07
0ff8770c-3098-4ac0-96a2-50d9b84a05d3	jmMkgbBkcG9peunFZlh70CxKm1+NnoQH2KFMltFHF+4=	e5941551-347f-4476-9d41-3cc235114950	5	f	f	2024-05-25 10:07:33.535074+07	2024-05-26 10:07:33.535075+07
c6241f62-f011-47f2-9492-fd409fcb4748	D5ESxYITGAZjXamGICu0aHdXKzckQKQXG5Aw6Kh6JLM=	56547760-76ed-4c47-813a-c0b40d91770c	5	f	f	2024-05-25 15:59:52.071906+07	2024-05-26 15:59:52.071908+07
58703623-eceb-4fbd-bb84-c2226d072a46	4rjvZHPp6PVupgFT5f5sJ2tqdnEtLKBp6r2UguxL+EY=	b3d63cd5-210e-43d4-9f97-04dac0cb1199	5	f	f	2024-05-25 18:58:48.963181+07	2024-05-26 18:58:48.963182+07
b6fa553c-37b6-472f-ba6c-7e06ef98c454	ebh/g0x+BOaThX2ZsSp7OMFDBKtBOa0hyTwZbna7s40=	74008a21-9b00-4051-95d2-66502085ca3b	5	f	f	2024-05-25 19:10:24.62667+07	2024-05-26 19:10:24.626671+07
fdb91159-1e92-4c25-a21d-d43ec56e9251	W0bbqxsyowGZdBjXE7Xfzd3v8UhnGln4od39YMgDaEQ=	aae5b8f3-06dd-4a23-9426-bff865f2c4be	5	f	f	2024-05-25 19:17:10.342224+07	2024-05-26 19:17:10.342225+07
2c6a42cd-c7e1-41f0-ae85-456398ee32da	vlzsQfTI4CCTSlxUl+ukt/6sPC+5gqcGm4jNGDpZNb0=	c44efbce-58d8-4770-9c65-0fea96a3437f	5	f	f	2024-05-25 19:17:32.876841+07	2024-05-26 19:17:32.876842+07
ad78e47e-9ab0-4a08-91db-594c3940afbd	5XrlCHwFdQcPytcIcICLXO87qhqYQLdhCWN2FvrGnYs=	63a2d0f8-f00e-41b4-9630-3032d6d2dcd8	5	f	f	2024-05-25 20:22:36.932552+07	2024-05-26 20:22:36.932621+07
c4a2dd27-a78e-4515-885e-334849395723	ZqrOKrbExWtm8W1PeUNQfZLa8084XqA7SRTqHO4WbAU=	a7a1d45b-950d-4557-89a3-ddfb636bc738	12	f	f	2024-05-25 20:23:36.456521+07	2024-05-26 20:23:36.456522+07
a55f4f67-81b6-45af-85b2-4e42a7c29d47	0XPNtdicPgMN5sAXptuWn6PiM3vseuCaBGCzvQjQcUU=	1d8c846e-8d16-4468-bd18-2e08a1a2abc6	5	f	f	2024-05-25 21:00:42.261787+07	2024-05-26 21:00:42.261857+07
e5def174-92c8-4d18-b4b0-e024d897673f	fEv1LRgoM+JKTrCcNziRP7ZOPDWwkNT3Ia5SOaI6IJc=	6ba5220c-4809-4d8b-bd21-c8b268da4c5c	6	f	f	2024-05-25 21:55:54.225788+07	2024-05-26 21:55:54.225879+07
4356ee31-58b2-4d1a-98cd-423b8ae3209b	CIK1EbwPiZJhwTPN2Btw1KhRVcLCBCSh3qtvYNKTwmQ=	719673bf-5a74-43a4-9cab-7ecdffded062	5	f	f	2024-05-26 01:13:22.479387+07	2024-05-27 01:13:22.47944+07
c5696e6a-7e4e-4029-a0d2-a54dcc2c6c8c	TXizYH/AnGWwdG28OzfrQ1vDxSaWXGtbiBLnk2wvcg4=	f2361452-6344-4dbb-aa60-5980c2b87804	5	f	f	2024-05-28 09:56:13.999561+07	2024-05-29 09:56:13.99962+07
7079c30d-5f5f-4b6b-a59e-5578bca5ddfe	ood52lk48mY4fic+rL1+7QAKabfURjPOKnHn2BkbvOk=	e14afacf-a84e-441c-8c2f-67e57da24fd9	10	f	f	2024-05-29 21:07:41.635704+07	2024-05-30 21:07:41.635707+07
fa9f81e4-b5cc-44e5-a5d6-f2f112c6af08	u/qaQZiKPO7F8BGnHBP9k+Tt5+BRNC+US3FSg0B4yps=	53da4e8c-ec15-45b5-9bbb-a1556c731f8e	6	f	f	2024-05-29 21:07:56.039229+07	2024-05-30 21:07:56.039231+07
504366d7-b425-4f29-be41-79768baa3932	cNU5r6HsVsejxWjwu5woy5qOzkIGdmVuOfU9GF0tYBw=	fb3a63c2-a075-43ad-b1b1-4f4392dd668f	9	f	f	2024-05-29 21:08:07.038596+07	2024-05-30 21:08:07.038599+07
b1e66e22-8cc7-4b60-8b40-a4de7ecbcd5e	X1ZBfBydpVLL2C+69ODYMbkuOCIsg7aEszvXPtxsbz4=	c997eff6-24e5-45ef-84d5-7b23e6e10dc9	5	f	f	2024-06-01 15:39:16.21347+07	2024-06-02 15:39:16.213513+07
530f940c-775f-452c-af9c-4c8227269f37	c2DVPs/P47O4zqjqT5dD3w/HHy/P7NeQLaV9SadtzfM=	9376efbe-8c7e-403b-b8a8-017fb4f9a1a8	5	f	f	2025-06-01 17:00:06.81431+07	2025-06-02 17:00:06.814355+07
9d91e110-3d09-4acf-8bdc-ce67317cc981	GUAVat6JsKvRcYto3YRHKfb8ztWHsf15w943BmCPc4U=	839a7ba0-7a86-4904-a298-f1c957579107	8	f	f	2025-06-01 17:07:03.968336+07	2025-06-02 17:07:03.968338+07
1c94fa4c-99ef-48dd-85e9-b1ae83a59644	OIUl3SsI17BK9lR+B9Drayp/rBG67O1/VYq6dexqJR8=	2a6b9e05-1978-4bbd-9474-193b0e096e74	5	f	f	2025-06-01 18:46:14.768569+07	2025-06-02 18:46:14.768676+07
e96dacb7-2e26-4e75-8804-b7a18d03f3c8	5pPbxiPW5qfZabcqrQTFsCIRiNWCCQN+PxwML2P89Xg=	658a8700-fd59-4bfd-8543-9c4948c8f49b	5	f	f	2025-06-01 18:58:25.815117+07	2025-06-02 18:58:25.815168+07
a94fc53e-258b-4233-8a61-d18bfdb55dc2	PJb89Pto3MPAS1LUvPN682DgiCXVoTjMjEo9UYSmBqc=	7ad9bd8e-27f4-4125-9acf-57a7219cd4f9	5	f	f	2025-06-01 19:45:57.452125+07	2025-06-02 19:45:57.452169+07
f479093d-677e-4f6a-9426-ca8b72ee2de8	xPCBt/mSY/MyZame4udOxHA+E9Yp07bsQEGMtVaabig=	59149a6c-2dad-4b60-9c34-620eb534bb36	5	f	f	2025-06-01 23:18:20.408889+07	2025-06-02 23:18:20.408967+07
68eab6d0-ef37-47f9-aa87-b0474cfeb0fc	HsOQRF4hk9VTrVQKB8tF8718nQ7ilO5WEEbYrY398rk=	58e95515-5cb9-4ef6-9c47-800671d88a6f	8	f	f	2025-06-01 23:18:41.586117+07	2025-06-02 23:18:41.586119+07
1b59e5ee-9383-4503-8c2d-4b33be96430c	I8vCjTUKVlfufmL8UOBnDcE6gjwkOkz7WBE5g0Xm0eg=	b38a7c40-6432-4ef5-9885-d0dfe9eee89c	8	f	f	2025-06-04 12:39:51.108528+07	2025-06-05 12:39:51.10872+07
4da2f021-26b2-43ab-a2dc-26495482a622	ry+/w5zaYxSFHXRP3jw8RnEQt9tsE2yal1Y0NH39pz0=	2ab7369c-a47a-4dce-a31a-c979e0b8c206	5	f	f	2025-06-04 12:39:53.342116+07	2025-06-05 12:39:53.342117+07
\.


--
-- Data for Name: RoomMemberInfos; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."RoomMemberInfos" ("Id", "UserId", "RoomId", "UnseenMessageCount", "canDisplayRoom", "canShowNofitication", "FirstUnseenMessageId") FROM stdin;
165	5	103	0	t	f	\N
313	7	102	3	t	f	4018
314	10	102	3	t	f	4018
158	8	98	30	t	f	4105
210	4	111	0	t	f	\N
304	6	102	11	t	f	4006
303	8	102	0	t	f	\N
157	5	98	0	t	f	\N
166	9	103	1	t	f	4019
163	5	102	0	t	f	\N
\.


--
-- Data for Name: Rooms; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Rooms" ("Id", "LastMessageId", "FirstMessageId", "IsGroup") FROM stdin;
102	4089	3847	t
103	4019	4019	f
111	\N	\N	t
98	4134	3846	f
\.


--
-- Data for Name: Users; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public."Users" ("Id", "Fullname", "Password", "Salt", "Email", "Avatar", "IsOnline", "CreatedAt") FROM stdin;
4	onetwo	oYYwDTMVTI5vbUraZhVi70WJAZr71Oa0d3fCxn/RA6/zKFI/wVuGrUx8Ptqf5VrUcib3Nqh8SFodQDSZ38uHXQ==	7Sb3u56Y	mothai@gmail.com	\N	f	2024-04-20 17:09:01.50015+07
6	two	MGUUJFrj6EMYV5mwIQXht4Q56z2ciJgQucMDhJF8BaNfJDW8R7U1X7zDVSdhfg/vbyk7urqUDuAEcNMrXslppw==	u2m1NSxe	hai@gmail.com	\N	f	2024-04-21 16:11:10.367523+07
7	three	uUvAhheKesHSeKmp5EneNtG1UHILOq/BmgXYPbmjadp4ZFEL5QHINFFJVd3PdaHIcCwU0QwW+7GAMi2nHQUiPQ==	YY27qSZX	ba@gmail.com	\N	f	2024-04-22 01:35:02.422824+07
9	five	zxo8rbcYJrYyrk5sQmYWrKDRKWlTDKKSVFnTw67G8iGen1UMh2PbKYpv1CPKP+UVmsVuJMJZxO4eV/Q1OSQDTQ==	6QoUbsOA	nam@gmail.com	https://storage.googleapis.com/chat_app_test/301e4df4-9f17-422a-af0b-22f853b927bc-cat.jpg	f	2024-05-10 21:57:13.516277+07
10	six	8yVyXRrXrRtYI1jgkxzif6Ne48aBoUIdxNnGZMLu+Ft8g9uSOuEb5YALB/ooddQzhtrI24K2FnRpUcSULsrs9A==	efqNvnhX	sau@gmail.com	https://storage.googleapis.com/chat_app_test/be3c19ff-9887-418f-b5a2-338e8a2f7081-380340.jpg	f	2024-05-12 19:01:41.990526+07
8	four	UNjNKs/fcT9VPnWMeGGu81FKu/HysjrMuVxCrVWkXbP+efwisKRgz9FOCj8XTfSRS6femXucShYsc1qPcs75Xw==	uDooJelM	bon@gmail.com	https://storage.googleapis.com/chat_app_test/d8bc552d-4b80-411e-a59f-2bb409836cc6-cat.jpg	f	2024-04-30 16:51:07.450137+07
11	test	bBcdVaS/R1qV7o7A69R48rXfDrcjEi+wDTU+aiL7MNe76bGQAH9intfJ3QrJ5oMy57u2QgvdMT0W5ohDKlmTVQ==	dvcIaFw8	test@gmail.com	https://storage.googleapis.com/chat_app_test/bd04683c-3654-4381-af40-39e230a7f956-cat.jpg	f	2024-05-25 16:10:22.10151+07
12	seven	txZbv61A8/qMso3WtDsklMM/b5O0I8Wclm1cmE4PNuME/FAkTYcQrGehO4o+h6Twk9JPNW9llXr1UwdLEy9+wA==	e7GHW69l	bay@gmail.com	https://storage.googleapis.com/chat_app_test/807ac03e-3919-4297-bccf-57bf5290d169-380340.jpg	f	2024-05-25 20:23:20.38473+07
5	One	kdrKk7/wlVRjMjjSzgGvBQFAU9VPnJqPE+lBimCZmgsUB8X5uP8JJWAmxHC4+nXkcuao2VOaz8Qy3pRVKhoxPA==	cisD8w5h	mot@gmail.com	https://storage.googleapis.com/chat_app_test/b290df74-59a0-4996-9c66-58e4ad77fc81-380381.jpg	f	2024-04-21 16:10:49.158127+07
\.


--
-- Data for Name: room_id; Type: TABLE DATA; Schema: public; Owner: postgres
--

COPY public.room_id ("RoomId") FROM stdin;
98
\.


--
-- Name: Emotions_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."Emotions_Id_seq"', 3, true);


--
-- Name: MessageDetail_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."MessageDetail_Id_seq"', 4603, true);


--
-- Name: PrivateRoomInfos_Id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."PrivateRoomInfos_Id_seq"', 316, true);


--
-- Name: PrivateRoom_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public."PrivateRoom_id_seq"', 123, true);


--
-- Name: blocklist_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.blocklist_id_seq', 1, false);


--
-- Name: friendships_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.friendships_id_seq', 1, false);


--
-- Name: privatemessages_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.privatemessages_id_seq', 4134, true);


--
-- Name: users_id_seq; Type: SEQUENCE SET; Schema: public; Owner: postgres
--

SELECT pg_catalog.setval('public.users_id_seq', 12, true);


--
-- Name: Reactions Emotions_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Reactions"
    ADD CONSTRAINT "Emotions_pkey" PRIMARY KEY ("Id");


--
-- Name: GroupInfos GroupInfo_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."GroupInfos"
    ADD CONSTRAINT "GroupInfo_pkey" PRIMARY KEY ("GroupId");


--
-- Name: MessageDetails MessageDetail_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT "MessageDetail_pkey" PRIMARY KEY ("Id");


--
-- Name: RoomMemberInfos PrivateRoomInfos_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT "PrivateRoomInfos_pkey" PRIMARY KEY ("Id");


--
-- Name: Rooms PrivateRoom_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Rooms"
    ADD CONSTRAINT "PrivateRoom_pkey" PRIMARY KEY ("Id");


--
-- Name: Blocklist blocklist_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Blocklist"
    ADD CONSTRAINT blocklist_pkey PRIMARY KEY ("Id");


--
-- Name: Friendships friendships_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT friendships_pkey PRIMARY KEY ("Id");


--
-- Name: Messages privatemessages_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT privatemessages_pkey PRIMARY KEY ("Id");


--
-- Name: RefreshToken refreshtoken_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshToken"
    ADD CONSTRAINT refreshtoken_pkey PRIMARY KEY ("Id");


--
-- Name: Messages unq_messages_id_roomid; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT unq_messages_id_roomid UNIQUE ("Id", "RoomId");


--
-- Name: RoomMemberInfos unq_user_room; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT unq_user_room UNIQUE ("UserId", "RoomId");


--
-- Name: Users users_email_key; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT users_email_key UNIQUE ("Email");


--
-- Name: Users users_pkey; Type: CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Users"
    ADD CONSTRAINT users_pkey PRIMARY KEY ("Id");


--
-- Name: idx_messages_id_roomid_senderid; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_messages_id_roomid_senderid ON public."Messages" USING btree ("Id", "RoomId", "SenderId");


--
-- Name: idx_messages_room_id; Type: INDEX; Schema: public; Owner: postgres
--

CREATE INDEX idx_messages_room_id ON public."Messages" USING btree ("RoomId");


--
-- Name: idx_unique_message_user; Type: INDEX; Schema: public; Owner: postgres
--

CREATE UNIQUE INDEX idx_unique_message_user ON public."MessageDetails" USING btree ("MessageId", "UserId");


--
-- Name: Messages fc_trig_messages_afterinsert; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_afterinsert AFTER INSERT ON public."Messages" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_afterinsert();


--
-- Name: Messages fc_trig_messages_beforedelete; Type: TRIGGER; Schema: public; Owner: postgres
--

CREATE TRIGGER fc_trig_messages_beforedelete BEFORE DELETE ON public."Messages" FOR EACH ROW EXECUTE FUNCTION public.fc_trig_messages_beforedelete();


--
-- Name: Blocklist fk_fs_user_blocked; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Blocklist"
    ADD CONSTRAINT fk_fs_user_blocked FOREIGN KEY ("BlockedId") REFERENCES public."Users"("Id");


--
-- Name: Blocklist fk_fs_user_blocker; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Blocklist"
    ADD CONSTRAINT fk_fs_user_blocker FOREIGN KEY ("BlockerId") REFERENCES public."Users"("Id");


--
-- Name: Friendships fk_fs_user_receiver; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT fk_fs_user_receiver FOREIGN KEY ("ReceiverId") REFERENCES public."Users"("Id");


--
-- Name: Friendships fk_fs_user_sender; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Friendships"
    ADD CONSTRAINT fk_fs_user_sender FOREIGN KEY ("SenderId") REFERENCES public."Users"("Id");


--
-- Name: GroupInfos fk_gi_room; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."GroupInfos"
    ADD CONSTRAINT fk_gi_room FOREIGN KEY ("GroupId") REFERENCES public."Rooms"("Id") ON DELETE CASCADE;


--
-- Name: GroupInfos fk_gi_user; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."GroupInfos"
    ADD CONSTRAINT fk_gi_user FOREIGN KEY ("GroupOwnerId") REFERENCES public."Users"("Id");


--
-- Name: MessageDetails fk_md_messages; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT fk_md_messages FOREIGN KEY ("MessageId", "RoomId") REFERENCES public."Messages"("Id", "RoomId") ON DELETE CASCADE;


--
-- Name: MessageDetails fk_md_reactions; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT fk_md_reactions FOREIGN KEY ("ReactionId") REFERENCES public."Reactions"("Id");


--
-- Name: MessageDetails fk_md_users; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."MessageDetails"
    ADD CONSTRAINT fk_md_users FOREIGN KEY ("UserId") REFERENCES public."Users"("Id") ON DELETE CASCADE;


--
-- Name: Messages fk_message_message_quote; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT fk_message_message_quote FOREIGN KEY ("QuoteId") REFERENCES public."Messages"("Id");


--
-- Name: Messages fk_messages_room; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT fk_messages_room FOREIGN KEY ("RoomId") REFERENCES public."Rooms"("Id");


--
-- Name: Messages fk_messages_user_sender; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Messages"
    ADD CONSTRAINT fk_messages_user_sender FOREIGN KEY ("SenderId") REFERENCES public."Users"("Id");


--
-- Name: RoomMemberInfos fk_prinfo_messages_first_unseen; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_messages_first_unseen FOREIGN KEY ("FirstUnseenMessageId") REFERENCES public."Messages"("Id");


--
-- Name: RoomMemberInfos fk_prinfo_room; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_room FOREIGN KEY ("RoomId") REFERENCES public."Rooms"("Id") ON DELETE CASCADE;


--
-- Name: RoomMemberInfos fk_prinfo_user; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RoomMemberInfos"
    ADD CONSTRAINT fk_prinfo_user FOREIGN KEY ("UserId") REFERENCES public."Users"("Id");


--
-- Name: Rooms fk_room_message_first_message; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Rooms"
    ADD CONSTRAINT fk_room_message_first_message FOREIGN KEY ("FirstMessageId") REFERENCES public."Messages"("Id");


--
-- Name: Rooms fk_room_message_last_message; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."Rooms"
    ADD CONSTRAINT fk_room_message_last_message FOREIGN KEY ("LastMessageId") REFERENCES public."Messages"("Id");


--
-- Name: RefreshToken fk_rt_user; Type: FK CONSTRAINT; Schema: public; Owner: postgres
--

ALTER TABLE ONLY public."RefreshToken"
    ADD CONSTRAINT fk_rt_user FOREIGN KEY ("UserId") REFERENCES public."Users"("Id");


--
-- PostgreSQL database dump complete
--

