-- Commit 5: Row Level Security (RLS) policies and anonymized donor view

-- ---------------------------------------------------------------------------
-- Security helper functions (SECURITY DEFINER avoids RLS recursion on profiles)
-- ---------------------------------------------------------------------------

CREATE OR REPLACE FUNCTION public.current_user_role()
RETURNS public.user_role
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT role
    FROM public.profiles
    WHERE id = auth.uid();
$$;

CREATE OR REPLACE FUNCTION public.is_moderator_or_admin()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT COALESCE(public.current_user_role() IN ('moderator', 'admin'), false);
$$;

CREATE OR REPLACE FUNCTION public.is_admin()
RETURNS boolean
LANGUAGE sql
STABLE
SECURITY DEFINER
SET search_path = public
AS $$
    SELECT COALESCE(public.current_user_role() = 'admin', false);
$$;

-- Prevent non-admin users from escalating their own role via profile updates.
CREATE OR REPLACE FUNCTION public.prevent_role_self_escalation()
RETURNS trigger
LANGUAGE plpgsql
SECURITY DEFINER
SET search_path = public
AS $$
BEGIN
    IF NEW.role IS DISTINCT FROM OLD.role
       AND auth.role() <> 'service_role'
       AND NOT public.is_admin() THEN
        RAISE EXCEPTION 'Only administrators may change user roles.';
    END IF;
    RETURN NEW;
END;
$$;

CREATE TRIGGER trg_profiles_prevent_role_escalation
    BEFORE UPDATE ON public.profiles
    FOR EACH ROW
    EXECUTE FUNCTION public.prevent_role_self_escalation();

-- ---------------------------------------------------------------------------
-- Enable RLS on all core tables
-- ---------------------------------------------------------------------------

ALTER TABLE public.profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.donor_profiles ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.blood_requests ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.request_private ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.request_documents ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.moderation_reviews ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.donor_matches ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.notifications_outbox ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.consent_records ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.audit_log ENABLE ROW LEVEL SECURITY;

-- ---------------------------------------------------------------------------
-- profiles
-- ---------------------------------------------------------------------------

CREATE POLICY "profiles_select_own"
    ON public.profiles FOR SELECT
    TO authenticated
    USING (id = auth.uid());

CREATE POLICY "profiles_select_moderator_admin"
    ON public.profiles FOR SELECT
    TO authenticated
    USING (public.is_moderator_or_admin());

CREATE POLICY "profiles_insert_own"
    ON public.profiles FOR INSERT
    TO authenticated
    WITH CHECK (id = auth.uid() AND role IN ('requester', 'donor'));

CREATE POLICY "profiles_update_own"
    ON public.profiles FOR UPDATE
    TO authenticated
    USING (id = auth.uid())
    WITH CHECK (id = auth.uid());

CREATE POLICY "profiles_update_admin"
    ON public.profiles FOR UPDATE
    TO authenticated
    USING (public.is_admin())
    WITH CHECK (public.is_admin());

-- ---------------------------------------------------------------------------
-- donor_profiles (donors see and manage only their own profile)
-- ---------------------------------------------------------------------------

CREATE POLICY "donor_profiles_select_own"
    ON public.donor_profiles FOR SELECT
    TO authenticated
    USING (user_id = auth.uid());

CREATE POLICY "donor_profiles_insert_own"
    ON public.donor_profiles FOR INSERT
    TO authenticated
    WITH CHECK (user_id = auth.uid());

CREATE POLICY "donor_profiles_update_own"
    ON public.donor_profiles FOR UPDATE
    TO authenticated
    USING (user_id = auth.uid())
    WITH CHECK (user_id = auth.uid());

-- ---------------------------------------------------------------------------
-- blood_requests
-- ---------------------------------------------------------------------------

CREATE POLICY "blood_requests_select_owner"
    ON public.blood_requests FOR SELECT
    TO authenticated
    USING (requester_id = auth.uid());

CREATE POLICY "blood_requests_select_moderator_admin"
    ON public.blood_requests FOR SELECT
    TO authenticated
    USING (
        public.is_moderator_or_admin()
        AND status IN ('pending_verification', 'approved', 'matching', 'fulfilled', 'denied', 'expired')
    );

CREATE POLICY "blood_requests_insert_owner"
    ON public.blood_requests FOR INSERT
    TO authenticated
    WITH CHECK (requester_id = auth.uid());

CREATE POLICY "blood_requests_update_owner"
    ON public.blood_requests FOR UPDATE
    TO authenticated
    USING (requester_id = auth.uid())
    WITH CHECK (requester_id = auth.uid());

CREATE POLICY "blood_requests_update_moderator_admin"
    ON public.blood_requests FOR UPDATE
    TO authenticated
    USING (public.is_moderator_or_admin())
    WITH CHECK (public.is_moderator_or_admin());

-- ---------------------------------------------------------------------------
-- request_private (owner and moderators/admins only)
-- ---------------------------------------------------------------------------

CREATE POLICY "request_private_select_owner_or_moderator"
    ON public.request_private FOR SELECT
    TO authenticated
    USING (
        public.is_moderator_or_admin()
        OR EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    );

CREATE POLICY "request_private_insert_owner"
    ON public.request_private FOR INSERT
    TO authenticated
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    );

CREATE POLICY "request_private_update_owner"
    ON public.request_private FOR UPDATE
    TO authenticated
    USING (
        EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    )
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    );

-- ---------------------------------------------------------------------------
-- request_documents (owner and moderators/admins only)
-- ---------------------------------------------------------------------------

CREATE POLICY "request_documents_select_owner_or_moderator"
    ON public.request_documents FOR SELECT
    TO authenticated
    USING (
        public.is_moderator_or_admin()
        OR EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    );

CREATE POLICY "request_documents_insert_owner"
    ON public.request_documents FOR INSERT
    TO authenticated
    WITH CHECK (
        EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    );

CREATE POLICY "request_documents_delete_owner"
    ON public.request_documents FOR DELETE
    TO authenticated
    USING (
        EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
              AND r.status IN ('draft', 'pending_verification')
        )
    );

-- ---------------------------------------------------------------------------
-- moderation_reviews
-- ---------------------------------------------------------------------------

CREATE POLICY "moderation_reviews_select_owner_or_moderator"
    ON public.moderation_reviews FOR SELECT
    TO authenticated
    USING (
        public.is_moderator_or_admin()
        OR EXISTS (
            SELECT 1
            FROM public.blood_requests r
            WHERE r.id = request_id
              AND r.requester_id = auth.uid()
        )
    );

CREATE POLICY "moderation_reviews_insert_moderator_admin"
    ON public.moderation_reviews FOR INSERT
    TO authenticated
    WITH CHECK (
        public.is_moderator_or_admin()
        AND moderator_id = auth.uid()
    );

-- ---------------------------------------------------------------------------
-- donor_matches (service_role writes only; donors can read their own rows)
-- ---------------------------------------------------------------------------

CREATE POLICY "donor_matches_select_own"
    ON public.donor_matches FOR SELECT
    TO authenticated
    USING (donor_id = auth.uid());

CREATE POLICY "donor_matches_select_moderator_admin"
    ON public.donor_matches FOR SELECT
    TO authenticated
    USING (public.is_moderator_or_admin());

-- No INSERT / UPDATE / DELETE policies on donor_matches for anon or authenticated:
-- only the service role (or SECURITY DEFINER RPCs added in later commits) can write.

-- ---------------------------------------------------------------------------
-- notifications_outbox (service_role only — zero client read/write policies)
-- ---------------------------------------------------------------------------

-- Intentionally no policies for anon or authenticated:
-- only service_role may read or write notifications_outbox.

-- ---------------------------------------------------------------------------
-- consent_records
-- ---------------------------------------------------------------------------

CREATE POLICY "consent_records_select_own"
    ON public.consent_records FOR SELECT
    TO authenticated
    USING (user_id = auth.uid());

CREATE POLICY "consent_records_insert_own"
    ON public.consent_records FOR INSERT
    TO authenticated
    WITH CHECK (user_id = auth.uid());

-- ---------------------------------------------------------------------------
-- audit_log (read-only for moderators/admins; writes via service_role only)
-- ---------------------------------------------------------------------------

CREATE POLICY "audit_log_select_moderator_admin"
    ON public.audit_log FOR SELECT
    TO authenticated
    USING (public.is_moderator_or_admin());

-- ---------------------------------------------------------------------------
-- Anonymized donor match view
-- Exposes ONLY non-identifying request details for matched donors.
-- Patient names, contact numbers, ward/room, and requester_id are excluded.
-- ---------------------------------------------------------------------------

CREATE OR REPLACE VIEW public.donor_matched_requests_view
WITH (security_invoker = false)
AS
SELECT
    dm.id AS match_id,
    dm.request_id,
    dm.donor_id,
    dm.status AS match_status,
    dm.notified_at,
    dm.responded_at,
    br.public_ref,
    br.blood_type_needed,
    br.component,
    br.units_needed,
    br.units_fulfilled,
    br.hospital_name,
    br.city,
    br.province,
    br.urgency,
    br.needed_by,
    br.status AS request_status,
    br.created_at AS request_created_at
FROM public.donor_matches dm
INNER JOIN public.blood_requests br
    ON br.id = dm.request_id
WHERE dm.donor_id = auth.uid();

REVOKE ALL ON public.donor_matched_requests_view FROM PUBLIC, anon;
GRANT SELECT ON public.donor_matched_requests_view TO authenticated;
