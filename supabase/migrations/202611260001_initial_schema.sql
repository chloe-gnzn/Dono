CREATE EXTENSION IF NOT EXISTS pgcrypto;

CREATE TYPE public.user_role AS ENUM (
    'requester',
    'donor',
    'moderator',
    'admin'
);

CREATE TYPE public.blood_type AS ENUM (
    'A+',
    'A-',
    'B+',
    'B-',
    'AB+',
    'AB-',
    'O+',
    'O-'
);

CREATE TYPE public.rh_factor AS ENUM (
    '+',
    '-'
);

CREATE TYPE public.blood_component AS ENUM (
    'whole_blood',
    'rbc',
    'platelets',
    'plasma'
);

CREATE TYPE public.request_urgency AS ENUM (
    'routine',
    'urgent',
    'critical'
);

CREATE TYPE public.request_status AS ENUM (
    'draft',
    'pending_verification',
    'approved',
    'matching',
    'fulfilled',
    'denied',
    'expired',
    'cancelled'
);

CREATE TYPE public.review_decision AS ENUM (
    'approved',
    'denied',
    'info_requested'
);

CREATE TYPE public.match_status AS ENUM (
    'notified',
    'accepted',
    'declined',
    'fulfilled',
    'expired'
);

CREATE TYPE public.notification_channel AS ENUM (
    'email',
    'sms',
    'in_app'
);

CREATE TYPE public.notification_status AS ENUM (
    'pending',
    'processing',
    'sent',
    'failed'
);

CREATE TYPE public.consent_type AS ENUM (
    'terms_of_service',
    'privacy_policy',
    'requester_declaration',
    'donor_eligibility_declaration',
    'data_processing'
);

CREATE OR REPLACE FUNCTION public.set_updated_at()
RETURNS trigger
LANGUAGE plpgsql
AS $$
BEGIN
    NEW.updated_at := now();
    RETURN NEW;
END;
$$;

CREATE TABLE public.profiles (
    id uuid PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    role public.user_role NOT NULL DEFAULT 'requester',
    full_name text NOT NULL DEFAULT '',
    phone text,
    city text,
    province text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_profiles_updated_at
    BEFORE UPDATE ON public.profiles
    FOR EACH ROW
    EXECUTE FUNCTION public.set_updated_at();

CREATE TABLE public.donor_profiles (
    user_id uuid PRIMARY KEY REFERENCES public.profiles(id) ON DELETE CASCADE,
    blood_type public.blood_type NOT NULL,
    rh public.rh_factor NOT NULL,
    last_donation_date date,
    is_available boolean NOT NULL DEFAULT true,
    date_of_birth date NOT NULL,
    weight_ok boolean NOT NULL DEFAULT false,
    medical_declaration boolean NOT NULL DEFAULT false,
    preferred_city text NOT NULL,
    preferred_province text NOT NULL,
    search_radius_km integer NOT NULL DEFAULT 25 CHECK (search_radius_km > 0 AND search_radius_km <= 500),
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_donor_profiles_updated_at
    BEFORE UPDATE ON public.donor_profiles
    FOR EACH ROW
    EXECUTE FUNCTION public.set_updated_at();

CREATE TABLE public.blood_requests (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    public_ref text NOT NULL UNIQUE DEFAULT ('BM-' || upper(substr(replace(gen_random_uuid()::text, '-', ''), 1, 8))),
    requester_id uuid NOT NULL REFERENCES public.profiles(id) ON DELETE RESTRICT,
    blood_type_needed public.blood_type NOT NULL,
    component public.blood_component NOT NULL DEFAULT 'whole_blood',
    units_needed integer NOT NULL CHECK (units_needed > 0 AND units_needed <= 20),
    units_fulfilled integer NOT NULL DEFAULT 0 CHECK (units_fulfilled >= 0 AND units_fulfilled <= units_needed),
    hospital_name text NOT NULL,
    city text NOT NULL,
    province text NOT NULL,
    urgency public.request_urgency NOT NULL DEFAULT 'urgent',
    needed_by timestamptz NOT NULL,
    status public.request_status NOT NULL DEFAULT 'draft',
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_blood_requests_updated_at
    BEFORE UPDATE ON public.blood_requests
    FOR EACH ROW
    EXECUTE FUNCTION public.set_updated_at();

CREATE TABLE public.request_private (
    request_id uuid PRIMARY KEY REFERENCES public.blood_requests(id) ON DELETE CASCADE,
    patient_name text NOT NULL,
    patient_relationship text NOT NULL,
    ward_room text,
    contact_number text NOT NULL,
    notes text,
    created_at timestamptz NOT NULL DEFAULT now(),
    updated_at timestamptz NOT NULL DEFAULT now()
);

CREATE TRIGGER trg_request_private_updated_at
    BEFORE UPDATE ON public.request_private
    FOR EACH ROW
    EXECUTE FUNCTION public.set_updated_at();

CREATE TABLE public.request_documents (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id uuid NOT NULL REFERENCES public.blood_requests(id) ON DELETE CASCADE,
    storage_path text NOT NULL UNIQUE,
    uploaded_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE public.moderation_reviews (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id uuid NOT NULL REFERENCES public.blood_requests(id) ON DELETE CASCADE,
    moderator_id uuid NOT NULL REFERENCES public.profiles(id) ON DELETE RESTRICT,
    decision public.review_decision NOT NULL,
    reason text,
    reviewed_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE public.donor_matches (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    request_id uuid NOT NULL REFERENCES public.blood_requests(id) ON DELETE CASCADE,
    donor_id uuid NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    status public.match_status NOT NULL DEFAULT 'notified',
    notified_at timestamptz NOT NULL DEFAULT now(),
    responded_at timestamptz,
    UNIQUE (request_id, donor_id)
);

CREATE TABLE public.notifications_outbox (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    channel public.notification_channel NOT NULL DEFAULT 'email',
    payload jsonb NOT NULL DEFAULT '{}'::jsonb,
    status public.notification_status NOT NULL DEFAULT 'pending',
    attempts integer NOT NULL DEFAULT 0 CHECK (attempts >= 0),
    sent_at timestamptz,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE public.consent_records (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL REFERENCES public.profiles(id) ON DELETE CASCADE,
    consent_type public.consent_type NOT NULL,
    version text NOT NULL,
    accepted_at timestamptz NOT NULL DEFAULT now(),
    ip inet
);

CREATE TABLE public.audit_log (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_id uuid REFERENCES public.profiles(id) ON DELETE SET NULL,
    action text NOT NULL,
    entity text NOT NULL,
    entity_id uuid,
    metadata jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at timestamptz NOT NULL DEFAULT now()
);

CREATE INDEX idx_blood_requests_type_city_status
    ON public.blood_requests (blood_type_needed, city, status);

CREATE INDEX idx_donor_profiles_type_city_available
    ON public.donor_profiles (blood_type, preferred_city, is_available);

CREATE INDEX idx_blood_requests_requester_created
    ON public.blood_requests (requester_id, created_at DESC);

CREATE INDEX idx_blood_requests_status_urgency_needed_by
    ON public.blood_requests (status, urgency, needed_by);

CREATE INDEX idx_request_documents_request_id
    ON public.request_documents (request_id);

CREATE INDEX idx_moderation_reviews_request_id
    ON public.moderation_reviews (request_id, reviewed_at DESC);

CREATE INDEX idx_donor_matches_donor_status
    ON public.donor_matches (donor_id, status);

CREATE INDEX idx_notifications_outbox_status_created
    ON public.notifications_outbox (status, created_at);

CREATE INDEX idx_consent_records_user_type
    ON public.consent_records (user_id, consent_type, accepted_at DESC);

CREATE INDEX idx_audit_log_entity_created
    ON public.audit_log (entity, entity_id, created_at DESC);
