-- Self-registration may only assign 'requester' or 'donor' (never 'moderator' or 'admin').

CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
    v_requested_role text;
    v_role public.user_role;
BEGIN
    v_requested_role := lower(coalesce(NEW.raw_user_meta_data ->> 'role', 'requester'));

    v_role := CASE
        WHEN v_requested_role = 'donor' THEN 'donor'::public.user_role
        ELSE 'requester'::public.user_role
    END;

    INSERT INTO public.profiles (id, role, full_name, phone, city, province)
    VALUES (
        NEW.id,
        v_role,
        coalesce(NEW.raw_user_meta_data ->> 'full_name', ''),
        nullif(NEW.raw_user_meta_data ->> 'phone', ''),
        nullif(NEW.raw_user_meta_data ->> 'city', ''),
        nullif(NEW.raw_user_meta_data ->> 'province', '')
    )
    ON CONFLICT (id) DO NOTHING;

    RETURN NEW;
END;
$$;

DROP TRIGGER IF EXISTS on_auth_user_created ON auth.users;

CREATE TRIGGER on_auth_user_created
    AFTER INSERT ON auth.users
    FOR EACH ROW
    EXECUTE FUNCTION public.handle_new_user();
