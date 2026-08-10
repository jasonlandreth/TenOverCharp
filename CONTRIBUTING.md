# Contributing to TenOverCharp

Thanks for taking a look — this is a personal hobby project, so the process here is intentionally lightweight.

## Before you start

This project connects to the Garmin R10's undocumented BLE protocol under the reverse-engineering interoperability exception (see [README.md](./README.md#disclaimer)). Contributions must stay within that scope:

- **No** circumventing Garmin licensing, subscriptions, or paid-feature gates
- **No** reproducing Garmin's own code or documentation
- Protocol changes should come from your own observed device behavior, not from Garmin's non-public materials

Issues or PRs proposing anything outside this scope will be closed.

## Reporting bugs

Open an issue with:

- What you expected to happen vs. what actually happened
- Your OS and .NET version
- Which transport you're using (`WindowsBleTransport`, `UniversalBleTransportAsync`, etc.)
- Console/debug output if the connection or protocol handshake is involved — this is usually the fastest way to spot what's going wrong

## Suggesting changes

Open an issue first for anything non-trivial (new features, protocol changes, breaking API changes) before putting time into a PR — happy to talk through the approach so you're not stuck redoing it.

Small fixes (typos, obvious bugs, doc corrections) are fine to PR directly.

## Pull requests

1. Fork the repo and branch off `master`.
2. Keep PRs focused — one change per PR is easier to review than a bundle of unrelated fixes.
3. Match the existing code style (see below).
4. Describe what you tested and on what hardware/OS, especially for anything touching the BLE transports — this stuff is genuinely hard to test without a physical R10.

## Code style

- XML doc summaries on public and private methods/properties.
- Braces `{ }` around all `if` statements, including single-line ones.
- Match existing file header conventions (see any existing `.cs` file for the copyright/attribution block).

## Questions

Open an issue — happy to help.
