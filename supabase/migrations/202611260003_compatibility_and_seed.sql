-- Commit 6: Blood compatibility matrix and reference seed data
-- Covers whole blood, RBC, platelets, and plasma (reversed ABO for plasma/platelets),
-- the 8 ABO/Rh blood types, Philippine city/province list, and consent document versions.

-- ---------------------------------------------------------------------------
-- 1. Reference blood types (8 ABO/Rh types)
-- ---------------------------------------------------------------------------

CREATE TABLE public.blood_types (
    code public.blood_type PRIMARY KEY,
    abo_group text NOT NULL CHECK (abo_group IN ('A', 'B', 'AB', 'O')),
    rh public.rh_factor NOT NULL,
    display_order smallint NOT NULL UNIQUE
);

INSERT INTO public.blood_types (code, abo_group, rh, display_order) VALUES
    ('O-',  'O',  '-', 1),
    ('O+',  'O',  '+', 2),
    ('A-',  'A',  '-', 3),
    ('A+',  'A',  '+', 4),
    ('B-',  'B',  '-', 5),
    ('B+',  'B',  '+', 6),
    ('AB-', 'AB', '-', 7),
    ('AB+', 'AB', '+', 8)
ON CONFLICT (code) DO NOTHING;

-- ---------------------------------------------------------------------------
-- 2. Blood compatibility table (recipient_type, donor_type, component)
-- ---------------------------------------------------------------------------

CREATE TABLE public.blood_compatibility (
    recipient_type public.blood_type NOT NULL REFERENCES public.blood_types(code),
    donor_type public.blood_type NOT NULL REFERENCES public.blood_types(code),
    component public.blood_component NOT NULL,
    PRIMARY KEY (recipient_type, donor_type, component)
);

CREATE INDEX idx_blood_compatibility_lookup
    ON public.blood_compatibility (recipient_type, component, donor_type);

-- Whole blood & Red Blood Cells (RBC): O- is universal donor, AB+ is universal recipient.
INSERT INTO public.blood_compatibility (recipient_type, donor_type, component)
SELECT r.code, d.code, c.component
FROM public.blood_types r
CROSS JOIN public.blood_types d
CROSS JOIN (VALUES ('whole_blood'::public.blood_component), ('rbc'::public.blood_component)) AS c(component)
WHERE
    -- ABO red-cell compatibility
    (
        d.abo_group = 'O'
        OR r.abo_group = 'AB'
        OR r.abo_group = d.abo_group
    )
    -- Rh compatibility: Rh- recipients receive only Rh-; Rh+ recipients receive Rh+ or Rh-.
    AND (
        r.rh = '+'
        OR d.rh = '-'
    )
ON CONFLICT DO NOTHING;

-- Plasma: compatibility is REVERSED (AB has no anti-A/anti-B antibodies -> universal plasma donor;
-- O recipients can receive plasma from any ABO group). Rh factor does not restrict plasma.
INSERT INTO public.blood_compatibility (recipient_type, donor_type, component)
SELECT r.code, d.code, 'plasma'::public.blood_component
FROM public.blood_types r
CROSS JOIN public.blood_types d
WHERE
    d.abo_group = 'AB'
    OR r.abo_group = 'O'
    OR r.abo_group = d.abo_group
ON CONFLICT DO NOTHING;

-- Platelets: reversed ABO compatibility (like plasma) with Rh-negative protection for Rh- recipients.
INSERT INTO public.blood_compatibility (recipient_type, donor_type, component)
SELECT r.code, d.code, 'platelets'::public.blood_component
FROM public.blood_types r
CROSS JOIN public.blood_types d
WHERE
    (
        d.abo_group = 'AB'
        OR r.abo_group = 'O'
        OR r.abo_group = d.abo_group
    )
    AND (
        r.rh = '+'
        OR d.rh = '-'
    )
ON CONFLICT DO NOTHING;

-- ---------------------------------------------------------------------------
-- 3. Philippine cities and provinces
-- ---------------------------------------------------------------------------

CREATE TABLE public.ph_locations (
    id serial PRIMARY KEY,
    region text NOT NULL,
    province text NOT NULL,
    city text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    UNIQUE (province, city)
);

CREATE INDEX idx_ph_locations_province_city
    ON public.ph_locations (province, city)
    WHERE is_active;

INSERT INTO public.ph_locations (region, province, city) VALUES
    -- National Capital Region (NCR)
    ('NCR', 'Metro Manila', 'Caloocan'),
    ('NCR', 'Metro Manila', 'Las Piñas'),
    ('NCR', 'Metro Manila', 'Makati'),
    ('NCR', 'Metro Manila', 'Malabon'),
    ('NCR', 'Metro Manila', 'Mandaluyong'),
    ('NCR', 'Metro Manila', 'Manila'),
    ('NCR', 'Metro Manila', 'Marikina'),
    ('NCR', 'Metro Manila', 'Muntinlupa'),
    ('NCR', 'Metro Manila', 'Navotas'),
    ('NCR', 'Metro Manila', 'Parañaque'),
    ('NCR', 'Metro Manila', 'Pasay'),
    ('NCR', 'Metro Manila', 'Pasig'),
    ('NCR', 'Metro Manila', 'Pateros'),
    ('NCR', 'Metro Manila', 'Quezon City'),
    ('NCR', 'Metro Manila', 'San Juan'),
    ('NCR', 'Metro Manila', 'Taguig'),
    ('NCR', 'Metro Manila', 'Valenzuela'),
    -- CAR & Northern/Central Luzon
    ('CAR', 'Benguet', 'Baguio'),
    ('Region I', 'Ilocos Norte', 'Laoag'),
    ('Region I', 'Pangasinan', 'Dagupan'),
    ('Region II', 'Cagayan', 'Tuguegarao'),
    ('Region III', 'Bulacan', 'Malolos'),
    ('Region III', 'Nueva Ecija', 'Cabanatuan'),
    ('Region III', 'Pampanga', 'Angeles'),
    ('Region III', 'Pampanga', 'San Fernando'),
    ('Region III', 'Tarlac', 'Tarlac City'),
    ('Region III', 'Zambales', 'Olongapo'),
    -- Southern Luzon & Bicol
    ('Region IV-A', 'Batangas', 'Batangas City'),
    ('Region IV-A', 'Batangas', 'Lipa'),
    ('Region IV-A', 'Cavite', 'Bacoor'),
    ('Region IV-A', 'Cavite', 'Dasmariñas'),
    ('Region IV-A', 'Cavite', 'Imus'),
    ('Region IV-A', 'Laguna', 'Biñan'),
    ('Region IV-A', 'Laguna', 'Calamba'),
    ('Region IV-A', 'Laguna', 'Santa Rosa'),
    ('Region IV-A', 'Quezon', 'Lucena'),
    ('Region IV-A', 'Rizal', 'Antipolo'),
    ('Region IV-B', 'Palawan', 'Puerto Princesa'),
    ('Region V', 'Albay', 'Legazpi'),
    ('Region V', 'Camarines Sur', 'Naga'),
    -- Visayas
    ('Region VI', 'Iloilo', 'Iloilo City'),
    ('Region VI', 'Negros Occidental', 'Bacolod'),
    ('Region VII', 'Bohol', 'Tagbilaran'),
    ('Region VII', 'Cebu', 'Cebu City'),
    ('Region VII', 'Cebu', 'Lapu-Lapu'),
    ('Region VII', 'Cebu', 'Mandaue'),
    ('Region VII', 'Negros Oriental', 'Dumaguete'),
    ('Region VIII', 'Leyte', 'Tacloban'),
    -- Mindanao
    ('Region IX', 'Zamboanga del Sur', 'Zamboanga City'),
    ('Region X', 'Misamis Oriental', 'Cagayan de Oro'),
    ('Region X', 'Lanao del Norte', 'Iligan'),
    ('Region XI', 'Davao del Sur', 'Davao City'),
    ('Region XII', 'South Cotabato', 'General Santos'),
    ('Region XII', 'South Cotabato', 'Koronadal'),
    ('Region XIII', 'Agusan del Norte', 'Butuan'),
    ('BARMM', 'Maguindanao del Norte', 'Cotabato City')
ON CONFLICT (province, city) DO NOTHING;

-- ---------------------------------------------------------------------------
-- 4. Consent document versions
-- ---------------------------------------------------------------------------

CREATE TABLE public.consent_documents (
    id uuid PRIMARY KEY DEFAULT gen_random_uuid(),
    consent_type public.consent_type NOT NULL,
    version text NOT NULL,
    title text NOT NULL,
    summary text NOT NULL,
    body_markdown text NOT NULL,
    is_active boolean NOT NULL DEFAULT true,
    published_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (consent_type, version)
);

CREATE UNIQUE INDEX idx_consent_documents_single_active
    ON public.consent_documents (consent_type)
    WHERE is_active;

INSERT INTO public.consent_documents (consent_type, version, title, summary, body_markdown, is_active) VALUES
    (
        'terms_of_service',
        '1.0',
        'BloodMatch Terms of Service',
        'Non-commercial intermediary agreement for all BloodMatch users.',
        'BloodMatch is a strictly non-commercial community platform connecting families seeking blood with volunteer donors. Buying, selling, or soliciting payment for blood or donor referrals is strictly prohibited.',
        true
    ),
    (
        'privacy_policy',
        '1.0',
        'BloodMatch Privacy Policy',
        'How BloodMatch protects patient identities and personal data.',
        'Patient names, room/ward details, and direct contact numbers are stored in restricted tables accessible only to the requesting account and verified moderators. Donors receive anonymized request details only.',
        true
    ),
    (
        'requester_declaration',
        '1.0',
        'Requester Authenticity Declaration',
        'Confirmation that the submitted hospital blood request is genuine.',
        'I confirm that this blood request is genuine, non-commercial, and supported by an authentic hospital blood request slip. I understand false submissions result in immediate account suspension.',
        true
    ),
    (
        'donor_eligibility_declaration',
        '1.0',
        'Volunteer Donor Eligibility Declaration',
        'Self-screening acknowledgement before receiving donor match notifications.',
        'I confirm that my stated blood type and health information are accurate to the best of my knowledge, and I understand that final medical screening is performed by the hospital blood bank before any donation.',
        true
    ),
    (
        'data_processing',
        '1.0',
        'Data Processing Consent (DPA)',
        'Consent under the Philippine Data Privacy Act (RA 10173).',
        'In accordance with the Philippine Data Privacy Act of 2012 (RA 10173), I consent to the collection and processing of my profile and blood matching records solely to coordinate voluntary blood donations.',
        true
    )
ON CONFLICT (consent_type, version) DO NOTHING;

-- ---------------------------------------------------------------------------
-- RLS for reference tables (public read-only; service_role writes)
-- ---------------------------------------------------------------------------

ALTER TABLE public.blood_types ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.blood_compatibility ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.ph_locations ENABLE ROW LEVEL SECURITY;
ALTER TABLE public.consent_documents ENABLE ROW LEVEL SECURITY;

CREATE POLICY "blood_types_public_read"
    ON public.blood_types FOR SELECT
    TO anon, authenticated
    USING (true);

CREATE POLICY "blood_compatibility_public_read"
    ON public.blood_compatibility FOR SELECT
    TO anon, authenticated
    USING (true);

CREATE POLICY "ph_locations_public_read"
    ON public.ph_locations FOR SELECT
    TO anon, authenticated
    USING (is_active = true);

CREATE POLICY "consent_documents_public_read"
    ON public.consent_documents FOR SELECT
    TO anon, authenticated
    USING (is_active = true);
