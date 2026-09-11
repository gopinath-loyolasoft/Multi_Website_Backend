UPDATE page_sections
SET content_json = '{
  "badge": "NAAC ''A+'' Grade Accredited",
  "heading": "Royal College of Arts and Science",
  "subheading": "Knowledge • Character • Society",
  "description": "Empowering minds, fostering innovation, and shaping ethical leaders for tomorrow''s world.",
  "ctaText": "Explore Programs",
  "ctaLink": "/courses",
  "secondaryCtaText": "Virtual Tour",
  "secondaryCtaLink": "/about",
  "showRightCard": true,
  "cardBadge": "2026-27 OPEN",
  "cardTitle": "Campus Highlights",
  "cardSubtitle": "rcm",
  "cardPrimaryButtonText": "Admissions & Eligibility",
  "cardPrimaryButtonUrl": "/admissions",
  "cardSecondaryButtonText": "Explore Academic Programs",
  "cardSecondaryButtonUrl": "/courses",
  "cardItems": [
    {
      "title": "Autonomous & Accredited",
      "desc": "Highest NAAC grade with updated industry-aligned curricula."
    },
    {
      "title": "Cutting-Edge Infrastructure",
      "desc": "50+ advanced research labs, smart classrooms & digital library."
    },
    {
      "title": "Placement & Mentorship",
      "desc": "Over 150+ top MNC recruiters visiting campus annually."
    }
  ],
  "pillars": [
    "NAAC Accredited",
    "Top Placement Record",
    "World-Class Faculty"
  ]
}'::jsonb
WHERE section_type = 'HERO';
