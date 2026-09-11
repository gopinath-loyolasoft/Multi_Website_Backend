DO $$
DECLARE
    v_page_id UUID;
    v_dept_cs UUID := gen_random_uuid();
    v_dept_com UUID := gen_random_uuid();
    v_dept_lit UUID := gen_random_uuid();
BEGIN
    -- 1. Configure Home Page Sections
    SELECT id INTO v_page_id FROM pages WHERE slug = 'home' LIMIT 1;

    IF v_page_id IS NOT NULL THEN
        DELETE FROM page_sections WHERE page_id = v_page_id;

        INSERT INTO page_sections (id, page_id, section_type, title, subtitle, content_json, sort_order, is_visible, created_at, updated_at)
        VALUES
        (gen_random_uuid(), v_page_id, 'HERO', 'Welcome to Royal College of Arts and Science', 'Empowering Minds, Shaping Futures', '{"primaryCtaText":"Explore Programs","primaryCtaUrl":"/courses","secondaryCtaText":"Virtual Tour","secondaryCtaUrl":"/about"}'::jsonb, 1, true, NOW(), NOW()),
        (gen_random_uuid(), v_page_id, 'STATISTICS', 'Campus at a Glance', 'Excellence in Numbers', '{}'::jsonb, 2, true, NOW(), NOW()),
        (gen_random_uuid(), v_page_id, 'QUOTE', 'Leadership Vision', 'A Message from our Leadership', '{}'::jsonb, 3, true, NOW(), NOW()),
        (gen_random_uuid(), v_page_id, 'DEPARTMENTS', 'Academic Divisions', 'Explore our faculties and specialized fields of study', '{"showFeaturedOnly":false}'::jsonb, 4, true, NOW(), NOW()),
        (gen_random_uuid(), v_page_id, 'COURSES', 'Featured Programs', 'Undergraduate & Postgraduate offerings', '{"limit":6}'::jsonb, 5, true, NOW(), NOW()),
        (gen_random_uuid(), v_page_id, 'NEWS', 'Campus News & Announcements', 'Latest happenings across our vibrant community', '{"limit":3}'::jsonb, 6, true, NOW(), NOW()),
        (gen_random_uuid(), v_page_id, 'EVENTS', 'Upcoming Campus Events', 'Workshops, seminars, and cultural festivals', '{"limit":3}'::jsonb, 7, true, NOW(), NOW());
    END IF;

    -- 2. Clear old engineering courses and departments
    DELETE FROM courses;
    DELETE FROM departments;

    -- 3. Insert Arts & Science Departments
    INSERT INTO departments (id, name, code, description, head_of_department, email, phone, is_active, created_at, updated_at)
    VALUES
    (v_dept_cs, 'Department of Computer Science & Mathematics', 'CSMATH', 'Cutting-edge computational thinking, software engineering, and applied mathematical sciences.', 'Dr. S. Ramanathan, Ph.D.', 'cs@rcm.edu', '+91 44 2834 1101', true, NOW(), NOW()),
    (v_dept_com, 'Department of Commerce & Management', 'COMMGMT', 'Fostering future business leaders, chartered professionals, and global entrepreneurs.', 'Dr. Meenakshi Sundaram, M.Com, Ph.D.', 'commerce@rcm.edu', '+91 44 2834 1102', true, NOW(), NOW()),
    (v_dept_lit, 'Department of English Literature & Humanities', 'ENGLIT', 'Developing critical inquiry, creative expression, and global cultural perspectives.', 'Dr. Elizabeth George, M.A., Ph.D.', 'humanities@rcm.edu', '+91 44 2834 1103', true, NOW(), NOW());

    -- 4. Insert Arts & Science Courses
    INSERT INTO courses (id, department_id, name, code, degree_level, duration_years, description, eligibility, fee_structure, is_active, created_at, updated_at)
    VALUES
    (gen_random_uuid(), v_dept_cs, 'B.Sc. Computer Science', 'BSC-CS', 'UNDERGRADUATE', 3.0, 'Core computer science curriculum covering algorithms, web systems, database design, and cloud architecture.', 'Higher Secondary (10+2) with Mathematics/Computer Science with min 60% aggregate.', 'INR 45,000 / semester', true, NOW(), NOW()),
    (gen_random_uuid(), v_dept_cs, 'M.Sc. Data Science & AI', 'MSC-DSAI', 'POSTGRADUATE', 2.0, 'Advanced postgraduate program specializing in predictive analytics, machine learning, and big data computing.', 'Bachelor degree in CS, IT, Mathematics, or Statistics with min 55% aggregate.', 'INR 60,000 / semester', true, NOW(), NOW()),
    (gen_random_uuid(), v_dept_com, 'B.Com. Honours (Finance & Analytics)', 'BCOM-HON', 'UNDERGRADUATE', 3.0, 'Rigorous commercial education with real-world corporate finance, fintech, and accounting standards.', 'Higher Secondary (10+2) in Commerce/Business Mathematics with min 65% aggregate.', 'INR 40,000 / semester', true, NOW(), NOW()),
    (gen_random_uuid(), v_dept_lit, 'B.A. English Literature & Media', 'BA-LIT', 'UNDERGRADUATE', 3.0, 'Comprehensive literary study combined with journalism, digital media communication, and rhetoric.', 'Higher Secondary (10+2) in any recognized stream with English proficiency.', 'INR 35,000 / semester', true, NOW(), NOW());

END $$;
