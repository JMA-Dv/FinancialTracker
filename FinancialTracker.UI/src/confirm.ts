import { ElMessageBox } from 'element-plus'

/** Asks the User to confirm; true when they did, false when they cancelled or dismissed it. */
export async function confirmAction(title: string, message: string, confirmText: string): Promise<boolean> {
  try {
    await ElMessageBox.confirm(message, title, {
      confirmButtonText: confirmText,
      cancelButtonText: 'Cancel',
      type: 'warning',
    })
    return true
  } catch (outcome) {
    // Element Plus rejects with 'cancel' or 'close' when the User declines; that is an answer, not a failure.
    if (outcome === 'cancel' || outcome === 'close') {
      return false
    }
    throw outcome
  }
}
