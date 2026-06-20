<template>
  <div class="page-container">
    <div class="page-header">
      <div>
        <h3 style="margin:0">塔吊管理</h3>
        <span style="color:#909399;font-size:13px">塔吊基础信息、运行状态、黑匣子设备联动</span>
      </div>
      <div class="header-actions">
        <el-select v-model="statusFilter" placeholder="状态筛选" clearable style="width:140px">
          <el-option v-for="(item, key) in TowerCraneStatusMap" :key="key" :label="item.label" :value="Number(key)" />
        </el-select>
        <el-input v-model="searchKeyword" placeholder="搜索塔吊编号/位置" clearable style="width:220px" :prefix-icon="Search" />
        <el-button type="primary" :icon="Plus" @click="openAddDialog">新增塔吊</el-button>
      </div>
    </div>

    <div v-if="linkageWarnings.length" class="linkage-warning">
      <div class="warning-title">
        <el-icon><Warning /></el-icon>
        联动风险提醒（{{ linkageWarnings.length }} 台塔吊存在安全限制）
      </div>
      <div class="warning-content">
        <el-timeline>
          <el-timeline-item
            v-for="(warn, i) in linkageWarnings"
            :key="i"
            :type="warn.type"
            :timestamp="warn.timestamp"
            placement="top"
          >
            <div style="font-weight:600">{{ warn.craneNo }} - {{ warn.title }}</div>
            <div style="color:#90601c">{{ warn.content }}</div>
          </el-timeline-item>
        </el-timeline>
      </div>
    </div>

    <el-row :gutter="16">
      <el-col :span="6" v-for="crane in filteredCranes" :key="crane.id">
        <el-card shadow="hover" class="crane-card" :body-style="{ padding: 0 }">
          <div class="crane-card-header" :class="`status-${crane.status}`">
            <div class="crane-status-dot" :class="`dot-${crane.status}`" />
            <div>
              <div class="crane-no">{{ crane.craneNo }}</div>
              <div class="crane-model">{{ crane.model }}</div>
            </div>
            <el-tag :type="TowerCraneStatusMap[crane.status].type" effect="dark" size="small">
              {{ TowerCraneStatusMap[crane.status].label }}
            </el-tag>
          </div>

          <div class="crane-card-body">
            <el-descriptions :column="1" size="small" border style="margin:16px">
              <el-descriptions-item label="位置">{{ crane.location }}</el-descriptions-item>
              <el-descriptions-item label="额定载荷">
                <span style="color:#409EFF;font-weight:600">{{ crane.ratedLoadCapacity }} 吨</span>
              </el-descriptions-item>
              <el-descriptions-item label="最大半径">{{ crane.maxRadius }} m</el-descriptions-item>
              <el-descriptions-item label="最大高度">{{ crane.maxHeight }} m</el-descriptions-item>
              <el-descriptions-item label="黑匣子编号">
                {{ crane.blackBoxDeviceId || '—' }}
              </el-descriptions-item>
              <el-descriptions-item label="下次检验">
                <span :style="{ color: isInspectionOverdue(crane) ? '#F56C6C' : '' }">
                  {{ crane.nextInspectionDate ? formatDate(crane.nextInspectionDate) : '—' }}
                  <el-tag v-if="isInspectionOverdue(crane)" type="danger" size="small" effect="dark" style="margin-left:4px">
                    已过期
                  </el-tag>
                </span>
              </el-descriptions-item>
            </el-descriptions>

            <div class="crane-stat-row">
              <div class="stat-item">
                <div class="stat-num" style="color:#409EFF">{{ crane.tasks?.filter(t => t.status === 4).length || 0 }}</div>
                <div class="stat-label">完成任务</div>
              </div>
              <div class="stat-item">
                <div class="stat-num" style="color:#67C23A">{{ crane.tasks?.filter(t => t.status === 3).length || 0 }}</div>
                <div class="stat-label">进行中</div>
              </div>
              <div class="stat-item">
                <div class="stat-num" style="color:#F56C6C">{{ crane.alarms?.filter(a => a.status !== 3 && a.status !== 4).length || 0 }}</div>
                <div class="stat-label">待处理报警</div>
              </div>
              <div class="stat-item">
                <div class="stat-num" style="color:#E6A23C">{{ crane.rectifications?.filter(r => r.status !== 4).length || 0 }}</div>
                <div class="stat-label">待整改</div>
              </div>
            </div>

            <div v-if="getCraneRestriction(crane)" class="restriction-tip">
              <el-icon><WarningFilled /></el-icon>
              {{ getCraneRestriction(crane) }}
            </div>

            <div class="crane-card-actions">
              <el-button type="primary" link size="small" @click="viewDetail(crane)">查看详情</el-button>
              <el-button type="success" link size="small" @click="viewAlarms(crane)">报警记录</el-button>
              <el-button type="warning" link size="small" @click="viewRectifications(crane)">整改记录</el-button>
              <el-dropdown trigger="click" @command="cmd => handleCommand(cmd, crane)">
                <el-button type="info" link size="small">
                  更多操作<el-icon class="el-icon--right"><ArrowDown /></el-icon>
                </el-button>
                <template #dropdown>
                  <el-dropdown-menu>
                    <el-dropdown-item command="edit">编辑</el-dropdown-item>
                    <el-dropdown-item command="status" divided>更新状态</el-dropdown-item>
                    <el-dropdown-item command="delete" style="color:#F56C6C">删除</el-dropdown-item>
                  </el-dropdown-menu>
                </template>
              </el-dropdown>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-col>

    <el-dialog v-model="craneDialogVisible" :title="isEdit ? '编辑塔吊' : '新增塔吊'" width="700px">
      <el-form :model="craneForm" label-width="110px" :rules="craneRules" ref="craneFormRef">
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="塔吊编号" prop="craneNo">
              <el-input v-model="craneForm.craneNo" placeholder="如 TC-001" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="塔吊型号" prop="model">
              <el-input v-model="craneForm.model" placeholder="如 QTZ80" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="12">
            <el-form-item label="出厂编号" prop="serialNo">
              <el-input v-model="craneForm.serialNo" />
            </el-form-item>
          </el-col>
          <el-col :span="12">
            <el-form-item label="安装位置" prop="location">
              <el-input v-model="craneForm.location" placeholder="如 A区1号楼南侧" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="额定载荷(吨)" prop="ratedLoadCapacity">
              <el-input-number v-model="craneForm.ratedLoadCapacity" :min="0" :precision="2" :step="0.1" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="最大半径(m)" prop="maxRadius">
              <el-input-number v-model="craneForm.maxRadius" :min="0" :precision="2" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="最大高度(m)" prop="maxHeight">
              <el-input-number v-model="craneForm.maxHeight" :min="0" :precision="2" style="width:100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-row :gutter="16">
          <el-col :span="8">
            <el-form-item label="安装日期">
              <el-date-picker v-model="craneForm.installDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="上次检验">
              <el-date-picker v-model="craneForm.lastInspectionDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
          <el-col :span="8">
            <el-form-item label="下次检验">
              <el-date-picker v-model="craneForm.nextInspectionDate" type="date" value-format="YYYY-MM-DD" style="width:100%" />
            </el-form-item>
          </el-col>
        </el-row>
        <el-form-item label="黑匣子设备ID">
          <el-input v-model="craneForm.blackBoxDeviceId" placeholder="黑匣子设备唯一标识" />
        </el-form-item>
      </el-form>
      <template #footer>
        <el-button @click="craneDialogVisible = false">取消</el-button>
        <el-button type="primary" :loading="saving" @click="saveCrane">保存</el-button>
      </template>
    </el-dialog>

    <el-drawer v-model="detailDrawerVisible" title="塔吊详情" size="600px">
      <div v-if="currentCrane" style="padding:0 20px">
        <el-descriptions title="基本信息" :column="2" border size="small">
          <el-descriptions-item label="编号">{{ currentCrane.craneNo }}</el-descriptions-item>
          <el-descriptions-item label="型号">{{ currentCrane.model }}</el-descriptions-item>
          <el-descriptions-item label="出厂编号">{{ currentCrane.serialNo }}</el-descriptions-item>
          <el-descriptions-item label="状态">
            <el-tag :type="TowerCraneStatusMap[currentCrane.status].type">
              {{ TowerCraneStatusMap[currentCrane.status].label }}
            </el-tag>
          </el-descriptions-item>
          <el-descriptions-item label="位置" :span="2">{{ currentCrane.location }}</el-descriptions-item>
          <el-descriptions-item label="额定载荷">{{ currentCrane.ratedLoadCapacity }} 吨</el-descriptions-item>
          <el-descriptions-item label="最大半径">{{ currentCrane.maxRadius }} m</el-descriptions-item>
          <el-descriptions-item label="最大高度">{{ currentCrane.maxHeight }} m</el-descriptions-item>
          <el-descriptions-item label="黑匣子">{{ currentCrane.blackBoxDeviceId || '—' }}</el-descriptions-item>
        </el-descriptions>

        <el-divider>联动状态</el-divider>
        <div class="linkage-success" v-if="!getCraneRestriction(currentCrane)">
          <div class="success-title">
            <el-icon><CircleCheck /></el-icon>状态正常
          </div>
          <div class="success-content">无未处理报警、无未关闭整改，可执行全风险等级任务。</div>
        </div>
        <div class="linkage-error" v-else>
          <div class="error-title">
            <el-icon><WarningFilled /></el-icon>存在限制
          </div>
          <div class="error-content">{{ getCraneRestriction(currentCrane) }}</div>
        </div>

        <el-divider>近期任务</el-divider>
        <el-table :data="currentCrane.tasks?.slice(0, 5) || []" size="small" stripe>
          <el-table-column label="任务号" prop="taskNo" width="140" />
          <el-table-column label="描述" prop="description" show-overflow-tooltip />
          <el-table-column label="风险" width="70" align="center">
            <template #default="{ row }">
              <el-tag :type="TaskRiskLevelMap[row.riskLevel].type" size="small">
                {{ TaskRiskLevelMap[row.riskLevel].label }}
              </el-tag>
            </template>
          </el-table-column>
          <el-table-column label="状态" width="90" align="center">
            <template #default="{ row }">
              <el-tag :type="TaskStatusMap[row.status].type" size="small">
                {{ TaskStatusMap[row.status].label }}
              </el-tag>
            </template>
          </el-table-column>
        </el-table>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { ref, computed, reactive, onMounted } from 'vue'
import { ElMessage, ElMessageBox, type FormInstance, type FormRules } from 'element-plus'
import {
  Plus, Search, Warning, ArrowDown, WarningFilled, CircleCheck
} from '@element-plus/icons-vue'
import dayjs from 'dayjs'
import api from '@/api'
import { useAppStore } from '@/store/app'
import type { TowerCrane } from '@/api'
import {
  TowerCraneStatusMap, TaskRiskLevelMap, TaskStatusMap
} from '@/utils/enumMaps'

const appStore = useAppStore()

const loading = ref(false)
const saving = ref(false)
const statusFilter = ref<number | null>(null)
const searchKeyword = ref('')

const craneDialogVisible = ref(false)
const detailDrawerVisible = ref(false)
const isEdit = ref(false)

const currentCrane = ref<TowerCrane | null>(null)

const craneForm = reactive<Partial<TowerCrane>>({
  craneNo: '', model: '', serialNo: '', location: '',
  status: 1, ratedLoadCapacity: 0, maxRadius: 0, maxHeight: 0, isActive: true
})

const craneFormRef = ref<FormInstance>()

const craneRules: FormRules = {
  craneNo: [{ required: true, message: '请输入塔吊编号', trigger: 'blur' }],
  model: [{ required: true, message: '请输入塔吊型号', trigger: 'blur' }],
  serialNo: [{ required: true, message: '请输入出厂编号', trigger: 'blur' }],
  location: [{ required: true, message: '请输入安装位置', trigger: 'blur' }],
  ratedLoadCapacity: [{ required: true, message: '请输入额定载荷', trigger: 'blur' }]
}

const filteredCranes = computed(() => {
  let list = appStore.towerCranes.filter(c => c.isActive)
  if (statusFilter.value !== null) {
    list = list.filter(c => c.status === statusFilter.value)
  }
  if (searchKeyword.value.trim()) {
    const kw = searchKeyword.value.trim().toLowerCase()
    list = list.filter(c =>
      c.craneNo.toLowerCase().includes(kw) || c.location.toLowerCase().includes(kw)
    )
  }
  return list
})

const linkageWarnings = computed(() => {
  const warnings: Array<{
    craneNo: string; title: string; content: string;
    type: 'warning' | 'danger' | 'primary'; timestamp: string
  }> = []
  filteredCranes.value.forEach(crane => {
    if (crane.status === 3) {
      warnings.push({
        craneNo: crane.craneNo,
        title: '预警状态',
        content: '塔吊处于预警状态，请检查报警和整改情况。',
        type: 'warning',
        timestamp: dayjs().format('HH:mm')
      })
    }
    if (crane.alarms?.some(a => a.status === 1 || a.status === 2)) {
      warnings.push({
        craneNo: crane.craneNo,
        title: '待处理报警',
        content: '存在未处理报警，超载/限位类报警将阻断吊装作业。',
        type: 'danger',
        timestamp: dayjs().format('HH:mm')
      })
    }
    if (crane.rectifications?.some(r => r.status !== 4)) {
      warnings.push({
        craneNo: crane.craneNo,
        title: '待完成整改',
        content: '整改关闭前仅允许执行低风险吊装任务。',
        type: 'warning',
        timestamp: dayjs().format('HH:mm')
      })
    }
    if (isInspectionOverdue(crane)) {
      warnings.push({
        craneNo: crane.craneNo,
        title: '检验过期',
        content: '已超过下次检验日期，请联系检验机构。',
        type: 'danger',
        timestamp: dayjs().format('HH:mm')
      })
    }
  })
  return warnings
})

const formatDate = (d: string) => dayjs(d).format('YYYY-MM-DD')

const isInspectionOverdue = (crane: TowerCrane) => {
  return crane.nextInspectionDate && dayjs(crane.nextInspectionDate).isBefore(dayjs())
}

const getCraneRestriction = (crane: TowerCrane) => {
  const restrictions: string[] = []
  const openRects = crane.rectifications?.filter(r => r.status !== 4)
  if (openRects && openRects.length > 0) {
    restrictions.push(`存在 ${openRects.length} 条未关闭整改，仅允许执行低风险任务`)
  }
  const pendingAlarms = crane.alarms?.filter(a =>
    (a.status === 1 || a.status === 2) &&
    (a.alarmType === 1 || a.alarmType === 2 || a.alarmType === 3 || a.alarmLevel === 3)
  )
  if (pendingAlarms && pendingAlarms.length > 0) {
    restrictions.push(`存在 ${pendingAlarms.length} 条阻断性报警，无法启动新任务`)
  }
  if (isInspectionOverdue(crane)) {
    restrictions.push('已超过下次检验日期')
  }
  return restrictions.join('；')
}

const openAddDialog = () => {
  isEdit.value = false
  Object.assign(craneForm, {
    id: undefined, craneNo: '', model: '', serialNo: '', location: '',
    status: 1, ratedLoadCapacity: 8.0, maxRadius: 50, maxHeight: 100,
    installDate: '', lastInspectionDate: '', nextInspectionDate: '',
    blackBoxDeviceId: '', isActive: true
  })
  craneDialogVisible.value = true
}

const saveCrane = async () => {
  await craneFormRef.value?.validate()
  saving.value = true
  try {
    if (isEdit.value && craneForm.id) {
      await api.towerCranes.update(craneForm.id, craneForm)
      ElMessage.success('编辑成功')
    } else {
      await api.towerCranes.create(craneForm)
      ElMessage.success('新增成功')
    }
    craneDialogVisible.value = false
    await appStore.fetchTowerCranes()
  } finally {
    saving.value = false
  }
}

const viewDetail = async (crane: TowerCrane) => {
  const { data } = await api.towerCranes.getById(crane.id)
  currentCrane.value = data
  detailDrawerVisible.value = true
}

const viewAlarms = (crane: TowerCrane) => {
  appStore.fetchAlarms().then(() => {
    window.location.hash = '/alarms'
  })
}

const viewRectifications = (crane: TowerCrane) => {
  appStore.fetchRectifications().then(() => {
    window.location.hash = '/rectifications'
  })
}

const handleCommand = async (cmd: string, crane: TowerCrane) => {
  if (cmd === 'edit') {
    isEdit.value = true
    const { data } = await api.towerCranes.getById(crane.id)
    Object.assign(craneForm, data)
    craneDialogVisible.value = true
  } else if (cmd === 'status') {
    ElMessageBox.prompt('请选择新状态', '更新状态', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      inputType: 'select',
      inputOptions: Object.entries(TowerCraneStatusMap).map(([k, v]) => ({
        value: k, label: v.label
      })),
      inputValue: String(crane.status)
    }).then(async ({ value }) => {
      await api.towerCranes.updateStatus(crane.id, Number(value))
      ElMessage.success('状态已更新')
      await appStore.fetchTowerCranes()
    }).catch(() => {})
  } else if (cmd === 'delete') {
    try {
      await ElMessageBox.confirm(`确定删除塔吊【${crane.craneNo}】？`, '确认', { type: 'warning' })
      await api.towerCranes.delete(crane.id)
      ElMessage.success('删除成功')
      await appStore.fetchTowerCranes()
    } catch { /* cancel */ }
  }
}

onMounted(async () => {
  loading.value = true
  try {
    await Promise.all([appStore.fetchTowerCranes(), appStore.fetchLiftingTasks()])
  } finally {
    loading.value = false
  }
})
</script>

<style lang="scss" scoped>
.crane-card {
  margin-bottom: 16px;
  transition: all 0.3s;

  &:hover {
    transform: translateY(-4px);
  }

  .crane-card-header {
    display: flex;
    align-items: center;
    gap: 12px;
    padding: 16px 20px;
    color: #fff;
    border-radius: 8px 8px 0 0;

    &.status-1 { background: linear-gradient(135deg, #67C23A 0%, #85ce61 100%) }
    &.status-2 { background: linear-gradient(135deg, #409EFF 0%, #66b1ff 100%) }
    &.status-3 { background: linear-gradient(135deg, #E6A23C 0%, #ebb563 100%) }
    &.status-4 { background: linear-gradient(135deg, #909399 0%, #a6a9ad 100%) }
    &.status-5 { background: linear-gradient(135deg, #F56C6C 0%, #f89898 100%) }

    .crane-status-dot {
      width: 10px;
      height: 10px;
      border-radius: 50%;
      background: rgba(255,255,255,0.9);
      animation: pulse 2s infinite;
    }

    .crane-no {
      font-size: 18px;
      font-weight: 700;
    }

    .crane-model {
      font-size: 12px;
      opacity: 0.9;
      margin-top: 2px;
    }
  }

  .crane-card-body {
    .crane-stat-row {
      display: flex;
      border-top: 1px solid #ebeef5;
      border-bottom: 1px solid #ebeef5;
      padding: 12px 0;
      margin: 0 16px;

      .stat-item {
        flex: 1;
        text-align: center;

        .stat-num {
          font-size: 22px;
          font-weight: 700;
        }

        .stat-label {
          font-size: 12px;
          color: #909399;
          margin-top: 2px;
        }
      }
    }

    .restriction-tip {
      margin: 12px 16px 0;
      padding: 8px 12px;
      background: #fef0f0;
      border: 1px solid #fde2e2;
      border-radius: 4px;
      color: #c45656;
      font-size: 12px;
      display: flex;
      align-items: center;
      gap: 6px;
    }

    .crane-card-actions {
      display: flex;
      justify-content: space-between;
      padding: 12px 16px;
    }
  }
}

@keyframes pulse {
  0% { box-shadow: 0 0 0 0 rgba(255,255,255,0.7) }
  70% { box-shadow: 0 0 0 8px rgba(255,255,255,0) }
  100% { box-shadow: 0 0 0 0 rgba(255,255,255,0) }
}
</style>
