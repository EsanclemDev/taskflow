
import { Component, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import {
  MAT_DIALOG_DATA,
  MatDialogModule,
  MatDialogRef
} from '@angular/material/dialog';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { Task, UpdateTask } from '../../models/task';

@Component({
  selector: 'app-edit-task-dialog',
  standalone: true,
  imports: [
    FormsModule,
    MatDialogModule,
    MatButtonModule,
    MatFormFieldModule,
    MatInputModule
  ],
  templateUrl: './edit-task-dialog.html',
  styleUrl: './edit-task-dialog.scss'
})
export class EditTaskDialog {
  private dialogRef = inject(
    MatDialogRef<EditTaskDialog, UpdateTask>
  );

  readonly task: Task = inject(MAT_DIALOG_DATA);

  form: UpdateTask = {
    title: this.task.title,
    description: this.task.description,
    isCompleted: this.task.isCompleted
  };

  save(): void {
    const title = this.form.title.trim();

    if (title.length < 3 || title.length > 100) {
      return;
    }

    this.dialogRef.close({
      ...this.form,
      title,
      description: this.form.description?.trim() || null
    });
  }

  cancel(): void {
    this.dialogRef.close();
  }
}
