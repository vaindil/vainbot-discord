CREATE TABLE key_value (
	"key" text NOT NULL PRIMARY KEY,
	value text NOT NULL
);

CREATE TABLE reminder (
	id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
	created_at timetz NOT NULL,
	fire_at timetz NOT NULL,
	user_id bigint NOT NULL,
	channel_id bigint NOT NULL,
	guild_id bigint NOT NULL,
	is_dm bool NOT NULL,
	message text NOT NULL
);

CREATE TABLE twitch_live_stream (
	twitch_user_id text NOT NULL PRIMARY KEY,
	twitch_stream_id text NOT NULL,
	started_at timetz NOT NULL,
	first_offline_at timetz NULL,
	twitch_login text NOT NULL,
	twitch_display_name text NOT NULL,
	viewer_count integer NOT NULL,
	title text NOT NULL,
	game_name text NOT NULL,
	game_id text NOT NULL,
	thumbnail_url text NOT NULL,
	profile_image_url text NOT NULL
);

CREATE TABLE twitch_stream_to_check (
	id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
	twitch_id text NOT NULL,
	username text NOT NULL,
	message_to_post text NOT NULL,
	channel_id bigint NOT NULL,
	guild_id bigint NOT NULL,
	is_embedded bool NOT NULL,
	is_deleted bool NOT NULL,
	current_message_id bigint NULL
);

CREATE TABLE twitter_to_check (
	id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
	twitter_username text NOT NULL,
	twitter_id bigint NOT NULL,
	include_retweets boolean NOT NULL,
	discord_guild_id bigint NOT NULL,
	discord_channel_id bigint NOT NULL
);

CREATE TABLE youtube_channel_to_check (
	id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
	username text NOT NULL,
	discord_guild_id bigint NOT NULL,
	discord_channel_id bigint NOT NULL,
	discord_message_to_post text NOT NULL,
	youtube_channel_id text NOT NULL,
	youtube_playlist_id text NOT NULL,
	is_deleted bool NOT NULL,
	latest_video_id text NULL,
	latest_video_uploaded_at timestamptz NULL,
	discord_message_id bigint NULL
);

CREATE TABLE tracked_user (
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    twitch_id text,
    discord_id bigint,
    is_moderator boolean NOT NULL
);

CREATE TABLE user_alias (
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id integer NOT NULL REFERENCES tracked_user(id),
    moderator_id integer NOT NULL REFERENCES tracked_user(id),
    added_at timestamptz NOT NULL,
    alias text NOT NULL
);

CREATE TABLE twitch_username_history (
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id integer NOT NULL REFERENCES tracked_user(id),
    logged_at timestamptz NOT NULL,
    username text NOT NULL
);

CREATE TABLE discord_username_history (
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id integer NOT NULL REFERENCES tracked_user(id),
    logged_at timestamptz NOT NULL,
    username text NOT NULL,
    discriminator text NOT NULL
);

CREATE TABLE action_taken (
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id integer NOT NULL REFERENCES tracked_user(id),
    moderator_id integer NOT NULL REFERENCES tracked_user(id),
    logged_at timestamptz NOT NULL,
    action_type text NOT NULL,
    duration_seconds integer NOT NULL,
    reason text
);

CREATE TABLE user_note (
    id integer NOT NULL GENERATED BY DEFAULT AS IDENTITY PRIMARY KEY,
    user_id integer NOT NULL REFERENCES tracked_user(id),
    moderator_id integer NOT NULL REFERENCES tracked_user(id),
    logged_at timestamptz NOT NULL,
    note text NOT NULL
);