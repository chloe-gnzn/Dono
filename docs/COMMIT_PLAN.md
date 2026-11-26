# Commit plan

Progress tracker. Tick a box when the commit has been applied to the repo.

## Phase 0: Foundation
- [x] 1. `chore: scaffold Blazor Web App and repo structure`
- [x] 2. `chore: integrate Tailwind CSS`
- [x] 3. `feat: base layout and shared UI components`

## Phase 1: Database and auth
- [x] 4. `feat(db): initial schema migration`
- [x] 5. `feat(db): Row Level Security policies`
- [x] 6. `feat(db): blood compatibility table and seed data`
- [x] 7. `feat: Supabase client and configuration`
- [ ] 8. `feat(auth): registration, login, logout`
- [ ] 9. `feat(auth): role-based authorization`

## Phase 2: Entry flow
- [ ] 10. `feat(ui): landing page`
- [ ] 11. `feat(flow): role selection and consent forms`

## Phase 3: Requester
- [ ] 12. `feat(requester): dashboard`
- [ ] 13. `feat(storage): private bucket and upload service`
- [ ] 14. `feat(requester): create blood request form`
- [ ] 15. `feat(requester): request detail and status tracker`

## Phase 4: Moderator
- [ ] 16. `feat(moderator): review queue`
- [ ] 17. `feat(moderator): review detail with document viewer`
- [ ] 18. `feat(moderator): approve / deny / request-info actions`
- [ ] 19. `feat(admin): moderator management`

## Phase 5: Donor
- [ ] 20. `feat(donor): onboarding and donor profile`
- [ ] 21. `feat(donor): dashboard`

## Phase 6: Matching and broadcast
- [ ] 22. `feat(db): compatible donor matching function`
- [ ] 23. `feat(matching): auto-trigger on approval`
- [ ] 24. `feat(notifications): outbox dispatcher and email`
- [ ] 25. `feat(donor): respond to request`

## Phase 7: Fulfillment
- [ ] 26. `feat: fulfillment confirmation`

## Phase 8: Hardening and release
- [ ] 27. `feat(security): audit, rate limiting, and abuse controls`
- [ ] 28. `test: unit and integration tests`
- [ ] 29. `chore: error handling, logging, and accessibility pass`
- [ ] 30. `chore: deployment and docs`
